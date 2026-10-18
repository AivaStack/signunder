using System.Reflection;
using System.Text.Json;

namespace Signunder.Rules;

/// <summary>
/// The rule packs shipped with this build (embedded from <c>packs/*.json</c>), checked
/// when loaded: a pack that names an unknown fact, requirement or source fails loudly
/// rather than answering wrongly.
/// </summary>
public sealed class RuleBook
{
    public static readonly IReadOnlySet<string> SetFacts = new HashSet<string> { "modalities", "roles", "markets", "platforms" };

    public static readonly IReadOnlySet<string> YesNoFacts = new HashSet<string>
    {
        "on_market_before_2026_08_02", "monthly_users_over_1m", "realistic", "public_interest_text", "code_signatory",
        "synthetic_performer_ad",
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> Values = new Dictionary<string, IReadOnlySet<string>>
    {
        ["modalities"] = new HashSet<string> { "image", "video", "audio", "text" },
        ["roles"] = new HashSet<string> { "provider", "deployer" },
        ["markets"] = new HashSet<string> { "eu", "california", "china", "south-korea", "india", "new-york" },
        ["platforms"] = new HashSet<string> { "tiktok", "meta", "youtube", "google-search", "google-ads", "linkedin" },
    };

    /// <summary>What a rule can ask for, in the order answers list them.</summary>
    public static readonly IReadOnlyList<string> Requirements =
    [
        "machine_readable_marking",
        "signed_metadata",
        "watermark",
        "latent_disclosure",
        "provenance_metadata",
        "aigc_metadata",
        "manifest_disclosure_option",
        "visible_label",
        "output_label",
        "prior_notice",
        "labelling_terms",
        "detection_tool",
        "interoperable_detection",
        "preserve_marks",
        "no_mark_removal",
        "unlawful_content_safeguards",
        "user_reminders",
        "visible_disclosure",
        "platform_label",
    ];

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public IReadOnlyList<RulePack> Packs { get; }

    public RuleBook(IEnumerable<RulePack> packs)
    {
        Packs = [.. packs.OrderBy(p => p.Pack, StringComparer.Ordinal)];
        foreach (var pack in Packs)
        {
            Validate(pack);
        }
    }

    /// <summary>The packs embedded in this assembly.</summary>
    public static RuleBook Embedded()
    {
        var assembly = typeof(RuleBook).Assembly;
        return new RuleBook(assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".json", StringComparison.Ordinal) && name.Contains(".packs.", StringComparison.Ordinal))
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                return JsonSerializer.Deserialize<RulePack>(stream, Json)
                    ?? throw new InvalidDataException($"{name} is empty.");
            }));
    }

    public static RulePack Parse(string json) =>
        JsonSerializer.Deserialize<RulePack>(json, Json) ?? throw new InvalidDataException("Empty pack.");

    private static void Validate(RulePack pack)
    {
        void Fail(string problem) => throw new InvalidDataException($"Pack {pack.Pack}@{pack.Version}: {problem}");

        if (string.IsNullOrWhiteSpace(pack.Pack) || string.IsNullOrWhiteSpace(pack.Version))
        {
            Fail("pack and version are required.");
        }
        if (pack.Kind is not ("law" or "code" or "platform"))
        {
            Fail($"unknown kind {pack.Kind}.");
        }
        var sources = pack.Sources.Select(s => s.Id).ToHashSet();
        var ids = new HashSet<string>();
        foreach (var rule in pack.Rules)
        {
            if (!ids.Add(rule.Id))
            {
                Fail($"rule {rule.Id} appears twice.");
            }
            if (rule.Strength is not ("law" or "code" or "platform-policy" or "platform-behaviour"))
            {
                Fail($"rule {rule.Id} has unknown strength {rule.Strength}.");
            }
            if (rule.SourceIds.Count == 0 || rule.SourceIds.Any(id => !sources.Contains(id)))
            {
                Fail($"rule {rule.Id} must cite the pack's sources.");
            }
            if (rule.Requires.Any(r => !Requirements.Contains(r)))
            {
                Fail($"rule {rule.Id} requires something unknown: {string.Join(", ", rule.Requires.Except(Requirements))}.");
            }
            if (!rule.Page.StartsWith('/'))
            {
                Fail($"rule {rule.Id} must link a signunder.com page.");
            }
            foreach (var condition in rule.When.Concat(rule.Grace is null ? [] : [rule.Grace.If]))
            {
                var known = SetFacts.Contains(condition.Fact)
                    ? condition.AnyOf is { Count: > 0 } values && values.All(Values[condition.Fact].Contains) && condition.Is is null
                    : YesNoFacts.Contains(condition.Fact) && condition.Is is not null && condition.AnyOf is null;
                if (!known)
                {
                    Fail($"rule {rule.Id} has a condition on {condition.Fact} that is not well formed.");
                }
            }
        }
    }
}
