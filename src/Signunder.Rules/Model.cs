namespace Signunder.Rules;

/// <summary>
/// One versioned set of rules from one jurisdiction, code or group of platforms
/// (SIGNUNDER.md §4.8). Rules are data: a change to the law or a policy is a new
/// version of a pack, never a code change.
/// </summary>
public sealed record RulePack(
    string Pack,
    string Version,
    string Title,
    // law, code or platform.
    string Kind,
    IReadOnlyList<Source> Sources,
    IReadOnlyList<Rule> Rules,
    string Changelog);

/// <summary>A primary source, and the day we last read it.</summary>
public sealed record Source(string Id, string Title, string Url, DateOnly Read);

public sealed record Rule(
    string Id,
    string Title,
    // What it means for the person asking, in a sentence or two.
    string Summary,
    // law binds; code binds signatories and shows everyone else what an
    // authority will regard as adequate; platform-policy is a platform's rule for
    // what you post; platform-behaviour is what the platform does with your
    // content, not a duty.
    string Strength,
    // All must hold for the rule to apply.
    IReadOnlyList<Condition> When,
    DateOnly EffectiveFrom,
    Grace? Grace,
    // Terms from RuleBook.Requirements.
    IReadOnlyList<string> Requires,
    // The source's own words, where a short quote carries the rule.
    string? Quote,
    IReadOnlyList<string> SourceIds,
    // The signunder.com page that explains the rule, with its anchor.
    string Page,
    // What part of the rule we could not confirm from a primary source, if any.
    string? Unverified);

/// <summary>A later date by which the rule must be met, when <see cref="If"/> holds.</summary>
public sealed record Grace(Condition If, DateOnly Until);

/// <summary>
/// A test on one answer. Set-valued facts (modalities, roles, markets, platforms) pass
/// when they share a value with <see cref="AnyOf"/>; yes/no facts when they equal
/// <see cref="Is"/>.
/// </summary>
public sealed record Condition(string Fact, bool? Is = null, IReadOnlyList<string>? AnyOf = null);

/// <summary>The checker's answers, and the day to evaluate them on.</summary>
public sealed record EvaluationContext(
    IReadOnlySet<string> Modalities,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Markets,
    IReadOnlySet<string> Platforms,
    bool OnMarketBefore20260802,
    bool MonthlyUsersOver1M,
    bool Realistic,
    bool PublicInterestText,
    bool CodeSignatory,
    DateOnly Date,
    // An advertisement featuring an AI-generated person who is not an identifiable real one.
    bool SyntheticPerformerAd = false);

public enum FindingStatus
{
    /// <summary>The duty already applies to you.</summary>
    InForce,

    /// <summary>The duty applies from a later date.</summary>
    Upcoming,
}

public sealed record Finding(
    string Pack,
    string PackVersion,
    Rule Rule,
    // The date the rule applies to you, grace included.
    DateOnly AppliesFrom,
    FindingStatus Status,
    // False for platform behaviour, and for the Code when you have not signed it.
    bool Binding,
    IReadOnlyList<Source> Sources);

public sealed record Evaluation(
    DateOnly Date,
    IReadOnlyList<Finding> Findings,
    // Everything the binding findings require, in a stable order.
    IReadOnlyList<string> Required,
    // Every pack consulted, as pack@version, so the answer can be reproduced.
    IReadOnlyList<string> PackVersions);
