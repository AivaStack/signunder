using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Signunder.Rules;

/// <summary>
/// The files published with signunder.com from <c>content/rules/</c>: every pack as JSON,
/// and the answers to the common situations in <c>scenarios.json</c>, evaluated by this
/// engine on the scenarios' date. Run by <c>make rules</c>; a test fails when the
/// committed files differ from what this would write, or when a situation no longer
/// triggers the rules its hand-written answer describes.
/// </summary>
public static class Export
{
    /// <summary>A common situation, its question and short answer, and the checker answers it stands for.</summary>
    public sealed record Scenario(
        string Slug,
        string Question,
        string Description,
        string Answer,
        ScenarioContext Context,
        IReadOnlyList<string> Expect);

    public sealed record ScenarioContext(
        IReadOnlyList<string> Modalities,
        IReadOnlyList<string>? Roles,
        IReadOnlyList<string>? Markets,
        IReadOnlyList<string>? Platforms,
        bool OnMarketBefore20260802,
        bool MonthlyUsersOver1M,
        bool Realistic,
        bool PublicInterestText,
        bool CodeSignatory,
        bool SyntheticPerformerAd = false);

    public sealed record Scenarios(DateOnly AsOf, IReadOnlyList<Scenario> Items);

    private static readonly JsonSerializerOptions Read = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Scenarios ParseScenarios(string json)
    {
        var root = JsonNode.Parse(json, documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true })!;
        var asOf = DateOnly.Parse((string)root["as_of"]!, CultureInfo.InvariantCulture);
        var items = root["scenarios"].Deserialize<List<Scenario>>(new JsonSerializerOptions(Read)
        {
            // The two flags whose names carry digits are spelt out in the file.
            PropertyNamingPolicy = new ScenarioNames(),
        })!;
        return new Scenarios(asOf, items);
    }

    public static EvaluationContext Context(ScenarioContext context, DateOnly date) => new(
        context.Modalities.ToHashSet(),
        (context.Roles ?? []).ToHashSet(),
        (context.Markets ?? []).ToHashSet(),
        (context.Platforms ?? []).ToHashSet(),
        context.OnMarketBefore20260802,
        context.MonthlyUsersOver1M,
        context.Realistic,
        context.PublicInterestText,
        context.CodeSignatory,
        date,
        context.SyntheticPerformerAd);

    /// <summary>The checker's query string for a situation, so its page can open the checker with the same answers.</summary>
    public static string CheckerQuery(ScenarioContext context, DateOnly date)
    {
        var parts = new List<string>();
        void List(string key, IReadOnlyList<string>? values)
        {
            if (values is { Count: > 0 })
            {
                parts.Add($"{key}={string.Join(",", values)}");
            }
        }
        void Flag(string key, bool value)
        {
            if (value)
            {
                parts.Add($"{key}=1");
            }
        }
        List("m", context.Modalities);
        List("r", context.Roles);
        List("w", context.Markets);
        List("p", context.Platforms);
        Flag("pre", context.OnMarketBefore20260802);
        Flag("big", context.MonthlyUsersOver1M);
        Flag("real", context.Realistic);
        Flag("pit", context.PublicInterestText);
        Flag("code", context.CodeSignatory);
        Flag("ad", context.SyntheticPerformerAd);
        parts.Add($"d={RulesJson.Day(date)}");
        return string.Join("&", parts);
    }

    /// <summary>Published path (under content/rules/) to file contents.</summary>
    public static IReadOnlyDictionary<string, string> Files(RuleBook book, Scenarios scenarios)
    {
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pack in book.Packs)
        {
            files[$"packs/{pack.Pack}.json"] = Text(RulesJson.Pack(pack));
        }
        files["packs/index.json"] = Text(new JsonObject
        {
            ["packs"] = new JsonArray([.. book.Packs.Select(p => (JsonNode)new JsonObject
            {
                ["pack"] = p.Pack,
                ["version"] = p.Version,
                ["title"] = p.Title,
                ["kind"] = p.Kind,
                ["rules"] = p.Rules.Count,
                ["file"] = $"{p.Pack}.json",
            })]),
        });

        files["answers.json"] = Text(new JsonObject
        {
            ["as_of"] = RulesJson.Day(scenarios.AsOf),
            ["pack_versions"] = new JsonArray([.. book.Packs.Select(p => JsonValue.Create($"{p.Pack}@{p.Version}"))]),
            ["answers"] = new JsonArray([.. scenarios.Items.Select(s => (JsonNode)new JsonObject
            {
                ["slug"] = s.Slug,
                ["question"] = s.Question,
                ["description"] = s.Description,
                ["answer"] = s.Answer,
                ["checker_query"] = CheckerQuery(s.Context, scenarios.AsOf),
                ["evaluation"] = RulesJson.Evaluation(Evaluator.Evaluate(book, Context(s.Context, scenarios.AsOf))),
            })]),
        });
        return files;
    }

    private static string Text(JsonNode node) => node.ToJsonString(RulesJson.Options) + "\n";

    private sealed class ScenarioNames : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name switch
        {
            nameof(ScenarioContext.OnMarketBefore20260802) => "on_market_before_2026_08_02",
            nameof(ScenarioContext.MonthlyUsersOver1M) => "monthly_users_over_1m",
            _ => SnakeCaseLower.ConvertName(name),
        };
    }
}
