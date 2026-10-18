using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Signunder.Rules;

namespace Signunder.Api.Verification;

/// <summary>
/// The free coverage checker's data (SIGNUNDER.md §1.2 Rules, §4.6): which marking rules
/// apply to what someone generates and where it goes, from the rule packs shipped with
/// this build. No key; the answers and the date are the whole input, and the answer names
/// the pack versions so it can be reproduced.
/// </summary>
public static class RulesEndpoints
{
    public static IServiceCollection AddRules(this IServiceCollection services) =>
        services.AddSingleton(_ => RuleBook.Embedded());

    public static void MapRules(this IEndpointRouteBuilder app)
    {
        var rules = app.MapGroup("/public/v1/rules")
            .RequireCors(Website.CorsPolicy)
            .RequireRateLimiting(RateLimits.Public);

        rules.MapGet("/packs", (RuleBook book) =>
            Results.Json(new JsonObject { ["packs"] = new JsonArray([.. book.Packs.Select(p => JsonSerializer.SerializeToNode(p, Json))]) }, Json));

        rules.MapPost("/evaluate", (EvaluateRequest request, RuleBook book, TimeProvider time) =>
        {
            var problems = new List<string>();
            IReadOnlySet<string> Set(string fact, string[]? values)
            {
                var set = (values ?? []).Select(v => v.Trim().ToLowerInvariant()).ToHashSet();
                var unknown = set.Except(RuleBook.Values[fact]).ToList();
                if (unknown.Count > 0)
                {
                    problems.Add($"{fact}: unknown {string.Join(", ", unknown)}; known are {string.Join(", ", RuleBook.Values[fact])}.");
                }
                return set;
            }

            var context = new EvaluationContext(
                Set("modalities", request.Modalities),
                Set("roles", request.Roles),
                Set("markets", request.Markets),
                Set("platforms", request.Platforms),
                request.OnMarketBefore20260802,
                request.MonthlyUsersOver1M,
                request.Realistic,
                request.PublicInterestText,
                request.CodeSignatory,
                request.Date ?? DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime));
            if (context.Modalities.Count == 0)
            {
                problems.Add("modalities: choose at least one of image, video, audio, text.");
            }
            if (problems.Count > 0)
            {
                return Results.Problem(string.Join(" ", problems), statusCode: 400);
            }

            var evaluation = Evaluator.Evaluate(book, context);
            return Results.Json(new JsonObject
            {
                ["date"] = evaluation.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["pack_versions"] = new JsonArray([.. evaluation.PackVersions.Select(v => JsonValue.Create(v))]),
                ["required"] = new JsonArray([.. evaluation.Required.Select(r => JsonValue.Create(r))]),
                ["findings"] = new JsonArray([.. evaluation.Findings.Select(FindingView)]),
            });
        });
    }

    /// <summary>The checker's answers. Booleans default to no.</summary>
    public sealed record EvaluateRequest(
        [property: JsonPropertyName("modalities")] string[]? Modalities,
        [property: JsonPropertyName("roles")] string[]? Roles,
        [property: JsonPropertyName("markets")] string[]? Markets,
        [property: JsonPropertyName("platforms")] string[]? Platforms,
        [property: JsonPropertyName("on_market_before_2026_08_02")] bool OnMarketBefore20260802,
        [property: JsonPropertyName("monthly_users_over_1m")] bool MonthlyUsersOver1M,
        [property: JsonPropertyName("realistic")] bool Realistic,
        [property: JsonPropertyName("public_interest_text")] bool PublicInterestText,
        [property: JsonPropertyName("code_signatory")] bool CodeSignatory,
        [property: JsonPropertyName("date")] DateOnly? Date);

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private static JsonObject FindingView(Finding finding) => new()
    {
        ["pack"] = finding.Pack,
        ["pack_version"] = finding.PackVersion,
        ["rule_id"] = finding.Rule.Id,
        ["title"] = finding.Rule.Title,
        ["summary"] = finding.Rule.Summary,
        ["strength"] = finding.Rule.Strength,
        ["binding"] = finding.Binding,
        ["status"] = finding.Status == FindingStatus.InForce ? "in_force" : "upcoming",
        ["applies_from"] = finding.AppliesFrom.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        ["requires"] = new JsonArray([.. finding.Rule.Requires.Select(r => JsonValue.Create(r))]),
        ["quote"] = finding.Rule.Quote,
        ["page"] = finding.Rule.Page,
        ["unverified"] = finding.Rule.Unverified,
        ["sources"] = new JsonArray([.. finding.Sources.Select(s => (JsonNode)new JsonObject
        {
            ["title"] = s.Title,
            ["url"] = s.Url,
            ["read"] = s.Read.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        })]),
    };
}
