using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Signunder.Api.Data;
using Signunder.Core.Registry;

namespace Signunder.Api.Auth;

/// <summary>
/// Authenticates <c>Authorization: Bearer su_…</c> against the api_key table and puts the
/// tenant and the key's scopes on the principal.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    RegistryDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string TenantClaim = "tenant_id";
    public const string ScopeClaim = "scope";
    public const string KeyHeader = "X-SignUnder-Key";

    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(1);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // In proxy mode Authorization carries the provider's key, so ours comes in
        // X-SignUnder-Key. Otherwise it is the Bearer token.
        string presented;
        if (Request.Headers.TryGetValue(KeyHeader, out var separate))
        {
            presented = separate.ToString().Trim();
        }
        else
        {
            var header = Request.Headers.Authorization.ToString();
            if (header.Length == 0)
            {
                return AuthenticateResult.NoResult();
            }
            const string bearer = "Bearer ";
            if (!header.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
            {
                return Failed("Expected a Bearer API key.");
            }
            presented = header[bearer.Length..].Trim();
        }
        if (!ApiKey.HasKeyShape(presented))
        {
            return Failed("Not a SignUnder API key.");
        }

        var hash = ApiKey.HashOf(presented);
        var key = await db.ApiKeys.AsNoTracking().SingleOrDefaultAsync(k => k.Hash == hash, Context.RequestAborted);
        if (key is null || key.RevokedAt is not null)
        {
            return Failed("Unknown or revoked API key.");
        }

        var now = DateTimeOffset.UtcNow;
        if (key.LastUsedAt is null || now - key.LastUsedAt > LastUsedResolution)
        {
            await db.ApiKeys.Where(k => k.Id == key.Id)
                .ExecuteUpdateAsync(k => k.SetProperty(x => x.LastUsedAt, now), Context.RequestAborted);
        }

        var claims = key.Scopes.Select(scope => new Claim(ScopeClaim, scope))
            .Append(new Claim(TenantClaim, key.TenantId.ToString()))
            .Append(new Claim("api_key_id", key.Id.ToString()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }

    /// <summary>Logs the refusal (never the key) so repeated failures can be seen and alerted on.</summary>
    private AuthenticateResult Failed(string reason)
    {
        Logger.LogWarning("API key refused: {Reason} Path {Path} from {Address}",
            reason, Request.Path, Context.Connection.RemoteIpAddress);
        return AuthenticateResult.Fail(reason);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = "Bearer";
        return base.HandleChallengeAsync(properties);
    }
}

public static class TenantPrincipal
{
    public static Guid TenantId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ApiKeyAuthenticationHandler.TenantClaim)!);
}
