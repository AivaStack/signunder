using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Signunder.Api.Data;
using Signunder.Core.Registry;

namespace Signunder.Api.Account;

/// <summary>
/// Self-serve sign-up for the dashboard: a person signed in with Supabase Auth makes their
/// tenant and manages its API keys. These routes take the Supabase session token, never an
/// API key, and are called from the website (<c>Website:AllowedOrigins</c>).
/// </summary>
public static class AccountEndpoints
{
    public const string Scheme = "SupabaseUser";
    public const string Policy = "account";

    /// <summary>Active API keys a tenant may hold at once.</summary>
    public const int MaxActiveKeys = 5;

    /// <summary>Slugs that would read as SignUnder's own pages or hosts.</summary>
    private static readonly HashSet<string> Reserved =
        ["demo", "signunder", "www", "api", "app", "admin", "dashboard", "verify", "public", "proxy", "media", "status", "docs", "test", "rules", "checker"];

    /// <summary>
    /// Trusts tokens issued by <c>SupabaseAuth:Issuer</c> (the project's
    /// <c>https://REF.supabase.co/auth/v1</c>), with signing keys from its discovery
    /// document. Without an issuer configured the account routes are not mapped.
    /// </summary>
    public static AuthenticationBuilder AddSupabaseUsers(this AuthenticationBuilder auth, IConfiguration config)
    {
        var issuer = config["SupabaseAuth:Issuer"];
        return auth.AddJwtBearer(Scheme, options =>
        {
            options.Authority = issuer;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidIssuer = issuer;
            options.TokenValidationParameters.ValidAudience = "authenticated";
            options.TokenValidationParameters.NameClaimType = "email";
        });
    }

    public static void AddAccountPolicy(this AuthorizationBuilder authorization) =>
        authorization.AddPolicy(Policy, policy => policy
            .AddAuthenticationSchemes(Scheme)
            .RequireAuthenticatedUser()
            .RequireClaim("role", "authenticated")
            .RequireClaim("sub")
            .RequireClaim("email"));

    public static void MapAccount(this IEndpointRouteBuilder app, IConfiguration config)
    {
        if (string.IsNullOrEmpty(config["SupabaseAuth:Issuer"]))
        {
            return;
        }
        var account = app.MapGroup("/v1/account")
            .RequireAuthorization(Policy)
            .RequireCors(Website.CorsPolicy)
            .RequireRateLimiting(RateLimits.PerKey);

        account.MapGet("", async (ClaimsPrincipal user, RegistryDbContext db, Signing.Quotas quotas, CancellationToken ct) =>
        {
            var member = await MemberAsync(db, user, ct);
            var tenant = member is null ? null : await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == member.TenantId, ct);
            var keys = member is null
                ? []
                : await db.ApiKeys.AsNoTracking().Where(k => k.TenantId == member.TenantId).OrderBy(k => k.CreatedAt).ToListAsync(ct);
            var usage = tenant is null ? null : await quotas.StateAsync(tenant.Id, ct);
            var identity = tenant is null ? null : await db.TenantIdentities.AsNoTracking().SingleOrDefaultAsync(i => i.TenantId == tenant.Id, ct);
            return Results.Json(new JsonObject
            {
                ["email"] = Email(user),
                ["tenant"] = tenant is null ? null : TenantView(tenant),
                ["identity"] = identity is null ? null : IdentityView(identity, tenant!),
                ["api_keys"] = new JsonArray([.. keys.Select(KeyView)]),
                ["usage"] = usage is null ? null : new JsonObject
                {
                    ["signed"] = usage.Used,
                    // Null when the plan has no monthly limit.
                    ["limit"] = usage.Limit,
                    ["period_start"] = usage.PeriodStart,
                    ["resets_at"] = usage.ResetsAt,
                },
            });
        });

        account.MapPost("/tenant", async (TenantRequest request, ClaimsPrincipal user, RegistryDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            var name = request.Name?.Trim() ?? "";
            if (name.Length is < 2 or > Tenant.MaxNameLength)
            {
                return Results.Problem($"Give a company name of 2 to {Tenant.MaxNameLength} characters.", statusCode: 400);
            }
            if (await MemberAsync(db, user, ct) is not null)
            {
                return Results.Problem("You already have a company on SignUnder.", statusCode: 409);
            }

            var now = DateTimeOffset.UtcNow;
            var tenant = new Tenant { Id = Guid.CreateVersion7(), LegalName = name, Slug = await FreeSlugAsync(db, name, ct), CreatedAt = now };
            if (await SignerConfig.NewTenantKeyAsync(config, tenant.Id, now, ct) is not { } signingKey)
            {
                return Results.Problem("Signing is not configured on this server.", statusCode: 503);
            }
            var (apiKey, plaintext) = ApiKey.Generate(tenant.Id, ApiScopes.All, now, live: config.GetValue<bool>("ApiKeys:Live"));

            db.Tenants.Add(tenant);
            db.TenantKeys.Add(signingKey);
            db.TenantMembers.Add(new TenantMember { TenantId = tenant.Id, UserId = UserId(user), Email = Email(user), CreatedAt = now });
            db.ApiKeys.Add(apiKey);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                // Two sign-ups at once from the same person, or two companies racing for a slug.
                return Results.Problem("That did not go through; please try again.", statusCode: 409);
            }

            return Results.Json(new JsonObject
            {
                ["tenant"] = TenantView(tenant),
                ["api_key"] = KeyView(apiKey, plaintext),
            }, statusCode: 201);
        });

        // The company's domain, proved by a DNS TXT record, so its name can go into its
        // signatures once Eoin has approved it (ADR 0014).
        account.MapPost("/domain", async (DomainRequest request, ClaimsPrincipal user, RegistryDbContext db, CancellationToken ct) =>
        {
            if (await MemberAsync(db, user, ct) is not { } member)
            {
                return Results.Problem("Create your company first.", statusCode: 404);
            }
            if (TenantIdentity.NormaliseDomain(request.Domain) is not { } domain)
            {
                return Results.Problem("Give your company's domain, for example acme.com.", statusCode: 400);
            }
            var tenant = await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == member.TenantId, ct);
            var identity = await db.TenantIdentities.SingleOrDefaultAsync(i => i.TenantId == member.TenantId, ct);
            if (identity is null)
            {
                identity = new TenantIdentity { TenantId = member.TenantId, Domain = domain, Token = TenantIdentity.NewToken() };
                db.TenantIdentities.Add(identity);
            }
            else if (identity.Domain != domain)
            {
                // A new domain starts over: verification, approval and certificate.
                identity.Domain = domain;
                identity.Token = TenantIdentity.NewToken();
                identity.DomainVerifiedAt = null;
                identity.ApprovedName = null;
                identity.ApprovedAt = null;
                identity.CertChainPem = null;
                identity.KeyRef = null;
            }
            identity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Json(IdentityView(identity, tenant));
        });

        account.MapPost("/domain/check", async (ClaimsPrincipal user, RegistryDbContext db, DomainVerifier dns, CancellationToken ct) =>
        {
            var member = await MemberAsync(db, user, ct);
            var identity = member is null ? null : await db.TenantIdentities.SingleOrDefaultAsync(i => i.TenantId == member.TenantId, ct);
            if (member is null || identity is null)
            {
                return Results.Problem("Add your domain first.", statusCode: 404);
            }
            var tenant = await db.Tenants.AsNoTracking().SingleAsync(t => t.Id == member.TenantId, ct);
            if (identity.DomainVerifiedAt is null)
            {
                var records = await dns.TxtRecordsAsync(identity.Domain, ct);
                if (!records.Contains(identity.TxtValue))
                {
                    return Results.Problem(
                        $"No TXT record \"{identity.TxtValue}\" on {identity.Domain} yet. DNS changes can take a few minutes to an hour to appear.",
                        statusCode: 409);
                }
                identity.DomainVerifiedAt = DateTimeOffset.UtcNow;
                identity.UpdatedAt = identity.DomainVerifiedAt.Value;
                await db.SaveChangesAsync(ct);
            }
            return Results.Json(IdentityView(identity, tenant));
        });

        account.MapPost("/keys", async (ClaimsPrincipal user, RegistryDbContext db, IConfiguration config, CancellationToken ct) =>
        {
            if (await MemberAsync(db, user, ct) is not { } member)
            {
                return Results.Problem("Create your company first.", statusCode: 404);
            }
            var active = await db.ApiKeys.CountAsync(k => k.TenantId == member.TenantId && k.RevokedAt == null, ct);
            if (active >= MaxActiveKeys)
            {
                return Results.Problem($"You have {MaxActiveKeys} active keys; revoke one first.", statusCode: 409);
            }
            var (apiKey, plaintext) = ApiKey.Generate(member.TenantId, ApiScopes.All, DateTimeOffset.UtcNow, live: config.GetValue<bool>("ApiKeys:Live"));
            db.ApiKeys.Add(apiKey);
            await db.SaveChangesAsync(ct);
            return Results.Json(KeyView(apiKey, plaintext), statusCode: 201);
        });

        account.MapPost("/keys/{id:guid}/revoke", async (Guid id, ClaimsPrincipal user, RegistryDbContext db, CancellationToken ct) =>
        {
            var member = await MemberAsync(db, user, ct);
            var key = member is null ? null : await db.ApiKeys.SingleOrDefaultAsync(k => k.Id == id && k.TenantId == member.TenantId, ct);
            if (key is null)
            {
                return Results.Problem($"No key {id}.", statusCode: 404);
            }
            key.RevokedAt ??= DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Json(KeyView(key));
        });
    }

    public sealed record TenantRequest(string? Name);

    public sealed record DomainRequest(string? Domain);

    private static JsonObject IdentityView(TenantIdentity identity, Tenant tenant) => new()
    {
        ["domain"] = identity.Domain,
        ["status"] = identity.Status(tenant.LegalName),
        ["txt_record"] = new JsonObject { ["host"] = identity.Domain, ["type"] = "TXT", ["value"] = identity.TxtValue },
        ["domain_verified_at"] = identity.DomainVerifiedAt,
        ["approved_name"] = identity.ApprovedName,
    };

    private static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue("sub")!);

    private static string Email(ClaimsPrincipal user) => user.FindFirstValue("email")!;

    private static Task<TenantMember?> MemberAsync(RegistryDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var userId = UserId(user);
        return db.TenantMembers.AsNoTracking().SingleOrDefaultAsync(m => m.UserId == userId, ct);
    }

    /// <summary>The name's slug, or that with a random suffix if it is taken or reserved.</summary>
    private static async Task<string> FreeSlugAsync(RegistryDbContext db, string name, CancellationToken ct)
    {
        var slug = Tenant.SlugFrom(name);
        if (slug.Length >= 2 && !Reserved.Contains(slug) && !await db.Tenants.AnyAsync(t => t.Slug == slug, ct))
        {
            return slug;
        }
        var stem = slug.Length >= 2 ? slug[..Math.Min(slug.Length, 33)] : "company";
        return $"{stem}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(3))}";
    }

    private static JsonObject TenantView(Tenant tenant) => new()
    {
        ["slug"] = tenant.Slug,
        ["name"] = tenant.LegalName,
        ["tier"] = tenant.Tier,
        ["created_at"] = tenant.CreatedAt,
    };

    private static JsonObject KeyView(ApiKey key) => KeyView(key, null);

    /// <summary>The key's plaintext appears only in the answer that creates it.</summary>
    private static JsonObject KeyView(ApiKey key, string? plaintext)
    {
        var view = new JsonObject
        {
            ["id"] = key.Id,
            ["prefix"] = key.Prefix,
            ["scopes"] = new JsonArray([.. key.Scopes.Select(s => JsonValue.Create(s))]),
            ["created_at"] = key.CreatedAt,
            ["last_used_at"] = key.LastUsedAt,
            ["revoked_at"] = key.RevokedAt,
        };
        if (plaintext is not null)
        {
            view["key"] = plaintext;
        }
        return view;
    }
}
