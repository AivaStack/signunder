namespace Signunder.Rules;

/// <summary>
/// Which rules apply to someone's answers, and what they require, on a given date
/// (docs/ARCHITECTURE.md §1.3, "Rules engine"). Deterministic: the same answers, date and pack
/// versions always give the same evaluation.
/// </summary>
public static class Evaluator
{
    public static Evaluation Evaluate(RuleBook book, EvaluationContext context)
    {
        var findings = new List<Finding>();
        foreach (var pack in book.Packs)
        {
            var sources = pack.Sources.ToDictionary(s => s.Id);
            foreach (var rule in pack.Rules.Where(r => r.When.All(c => Holds(c, context))))
            {
                var appliesFrom = rule.Grace is { } grace && Holds(grace.If, context) ? grace.Until : rule.EffectiveFrom;
                var binding = rule.Strength switch
                {
                    "law" or "platform-policy" => true,
                    "code" => context.CodeSignatory,
                    _ => false,
                };
                findings.Add(new Finding(
                    pack.Pack,
                    pack.Version,
                    rule,
                    appliesFrom,
                    appliesFrom <= context.Date ? FindingStatus.InForce : FindingStatus.Upcoming,
                    binding,
                    [.. rule.SourceIds.Select(id => sources[id])]));
            }
        }

        var required = findings.Where(f => f.Binding).SelectMany(f => f.Rule.Requires).ToHashSet();
        return new Evaluation(
            context.Date,
            [.. findings.OrderBy(f => f.AppliesFrom).ThenBy(f => f.Pack, StringComparer.Ordinal).ThenBy(f => f.Rule.Id, StringComparer.Ordinal)],
            [.. RuleBook.Requirements.Where(required.Contains)],
            [.. book.Packs.Select(p => $"{p.Pack}@{p.Version}")]);
    }

    private static bool Holds(Condition condition, EvaluationContext context) => condition.Fact switch
    {
        "modalities" => condition.AnyOf!.Any(context.Modalities.Contains),
        "roles" => condition.AnyOf!.Any(context.Roles.Contains),
        "markets" => condition.AnyOf!.Any(context.Markets.Contains),
        "platforms" => condition.AnyOf!.Any(context.Platforms.Contains),
        "on_market_before_2026_08_02" => context.OnMarketBefore20260802 == condition.Is,
        "monthly_users_over_1m" => context.MonthlyUsersOver1M == condition.Is,
        "realistic" => context.Realistic == condition.Is,
        "public_interest_text" => context.PublicInterestText == condition.Is,
        "code_signatory" => context.CodeSignatory == condition.Is,
        "synthetic_performer_ad" => context.SyntheticPerformerAd == condition.Is,
        _ => throw new InvalidOperationException($"Unknown fact {condition.Fact}."),
    };
}
