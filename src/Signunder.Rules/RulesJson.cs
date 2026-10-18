using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Signunder.Rules;

/// <summary>
/// The one JSON shape for packs and evaluations, shared by the public API and the files
/// published with the website, so the two can never describe a rule differently.
/// </summary>
public static class RulesJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        // Published files are read by people too: keep quotes and apostrophes as written.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static JsonNode Pack(RulePack pack) => JsonSerializer.SerializeToNode(pack, Options)!;

    public static JsonObject Evaluation(Evaluation evaluation) => new()
    {
        ["date"] = Day(evaluation.Date),
        ["pack_versions"] = new JsonArray([.. evaluation.PackVersions.Select(v => JsonValue.Create(v))]),
        ["required"] = new JsonArray([.. evaluation.Required.Select(r => JsonValue.Create(r))]),
        ["findings"] = new JsonArray([.. evaluation.Findings.Select(Finding)]),
    };

    public static JsonObject Finding(Finding finding) => new()
    {
        ["pack"] = finding.Pack,
        ["pack_version"] = finding.PackVersion,
        ["rule_id"] = finding.Rule.Id,
        ["title"] = finding.Rule.Title,
        ["summary"] = finding.Rule.Summary,
        ["strength"] = finding.Rule.Strength,
        ["binding"] = finding.Binding,
        ["status"] = finding.Status == FindingStatus.InForce ? "in_force" : "upcoming",
        ["applies_from"] = Day(finding.AppliesFrom),
        ["requires"] = new JsonArray([.. finding.Rule.Requires.Select(r => JsonValue.Create(r))]),
        ["quote"] = finding.Rule.Quote,
        ["page"] = finding.Rule.Page,
        ["unverified"] = finding.Rule.Unverified,
        ["sources"] = new JsonArray([.. finding.Sources.Select(s => (JsonNode)new JsonObject
        {
            ["title"] = s.Title,
            ["url"] = s.Url,
            ["read"] = Day(s.Read),
        })]),
    };
}
