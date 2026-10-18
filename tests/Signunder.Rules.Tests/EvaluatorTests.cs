namespace Signunder.Rules.Tests;

public class EvaluatorTests
{
    private static readonly RuleBook Book = RuleBook.Embedded();

    private static EvaluationContext Context(
        string[]? modalities = null,
        string[]? roles = null,
        string[]? markets = null,
        string[]? platforms = null,
        bool onMarketBefore = false,
        bool overMillion = false,
        bool realistic = false,
        bool publicInterestText = false,
        bool signatory = false,
        string date = "2026-10-18",
        bool syntheticPerformerAd = false) =>
        new(
            new HashSet<string>(modalities ?? ["image"]),
            new HashSet<string>(roles ?? ["provider"]),
            new HashSet<string>(markets ?? ["eu"]),
            new HashSet<string>(platforms ?? []),
            onMarketBefore,
            overMillion,
            realistic,
            publicInterestText,
            signatory,
            DateOnly.Parse(date, System.Globalization.CultureInfo.InvariantCulture),
            syntheticPerformerAd);

    private static Finding? Rule(Evaluation evaluation, string id) =>
        evaluation.Findings.SingleOrDefault(f => f.Rule.Id == id);

    [Fact]
    public void Every_embedded_pack_loads_and_names_its_version()
    {
        Assert.Equal(
            ["ca-ai-transparency-act", "cn-ai-labelling-measures", "eu-ai-act-art50", "eu-code-of-practice",
             "in-it-rules-synthetic", "kr-ai-basic-act", "ny-gbl-396b", "platform-labels"],
            Book.Packs.Select(p => p.Pack));
        var evaluation = Evaluator.Evaluate(Book, Context());
        Assert.Contains("eu-ai-act-art50@2026.10.12", evaluation.PackVersions);
    }

    [Fact]
    public void A_provider_in_the_eu_must_mark_and_the_code_is_expected_but_not_binding()
    {
        var evaluation = Evaluator.Evaluate(Book, Context());

        var marking = Rule(evaluation, "eu-50-2-marking")!;
        Assert.Equal(FindingStatus.InForce, marking.Status);
        Assert.Equal(new DateOnly(2026, 8, 2), marking.AppliesFrom);
        Assert.True(marking.Binding);
        Assert.False(Rule(evaluation, "code-1-1-1-signed-metadata")!.Binding);
        Assert.Equal(["machine_readable_marking"], evaluation.Required);
    }

    [Theory]
    [InlineData("2026-12-01", FindingStatus.Upcoming)]
    [InlineData("2026-12-02", FindingStatus.InForce)]
    public void A_system_on_the_market_before_2_august_has_until_2_december(string date, FindingStatus status)
    {
        var marking = Rule(Evaluator.Evaluate(Book, Context(onMarketBefore: true, date: date)), "eu-50-2-marking")!;

        Assert.Equal(new DateOnly(2026, 12, 2), marking.AppliesFrom);
        Assert.Equal(status, marking.Status);
    }

    [Fact]
    public void Signing_the_code_makes_its_layers_binding_and_detection_interoperable_from_february()
    {
        var before = Evaluator.Evaluate(Book, Context(signatory: true, date: "2027-02-01"));
        var on = Evaluator.Evaluate(Book, Context(signatory: true, date: "2027-02-02"));

        Assert.Equal(
            ["machine_readable_marking", "signed_metadata", "watermark", "detection_tool", "interoperable_detection", "preserve_marks"],
            before.Required);
        Assert.Equal(FindingStatus.Upcoming, Rule(before, "code-3-4-interoperable-detection")!.Status);
        Assert.Equal(FindingStatus.InForce, Rule(on, "code-3-4-interoperable-detection")!.Status);
    }

    [Theory]
    [InlineData(true, new[] { "image" }, 3)]
    [InlineData(false, new[] { "image" }, 0)]
    [InlineData(true, new[] { "text" }, 0)]
    public void California_binds_providers_over_a_million_users_and_not_text(bool overMillion, string[] modalities, int rules)
    {
        var evaluation = Evaluator.Evaluate(Book, Context(markets: ["california"], modalities: modalities, overMillion: overMillion));

        Assert.Equal(rules, evaluation.Findings.Count(f => f.Pack == "ca-ai-transparency-act"));
        if (rules > 0)
        {
            Assert.Equal(["latent_disclosure", "manifest_disclosure_option", "detection_tool"], evaluation.Required);
        }
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Publishing_realistic_media_in_the_eu_needs_a_visible_disclosure(bool realistic, bool applies)
    {
        var evaluation = Evaluator.Evaluate(Book, Context(roles: ["deployer"], modalities: ["video"], realistic: realistic));

        Assert.Equal(applies, Rule(evaluation, "eu-50-4-deepfake") is not null);
        Assert.Null(Rule(evaluation, "eu-50-2-marking"));
    }

    [Fact]
    public void Public_interest_text_needs_disclosure_only_when_asked_about()
    {
        Assert.NotNull(Rule(Evaluator.Evaluate(Book, Context(roles: ["deployer"], modalities: ["text"], publicInterestText: true)), "eu-50-4-text"));
        Assert.Null(Rule(Evaluator.Evaluate(Book, Context(roles: ["deployer"], modalities: ["text"])), "eu-50-4-text"));
    }

    [Fact]
    public void Platform_duties_follow_each_platform_s_own_scope()
    {
        // Meta asks for a label on realistic video and audio, not images, but labels marked images itself.
        var metaImage = Evaluator.Evaluate(Book, Context(markets: [], platforms: ["meta"], realistic: true));
        Assert.Null(Rule(metaImage, "meta-label-video-audio"));
        Assert.False(Rule(metaImage, "meta-reads-credentials")!.Binding);

        var metaVideo = Evaluator.Evaluate(Book, Context(markets: [], modalities: ["video"], platforms: ["meta"], realistic: true));
        Assert.True(Rule(metaVideo, "meta-label-video-audio")!.Binding);
        Assert.Equal(["platform_label"], metaVideo.Required);

        var tiktok = Evaluator.Evaluate(Book, Context(markets: [], platforms: ["tiktok"], realistic: true));
        Assert.NotNull(Rule(tiktok, "tiktok-label-realistic"));
        Assert.NotNull(Rule(tiktok, "tiktok-reads-credentials")!.Rule.Unverified);
    }

    [Fact]
    public void China_wants_both_labels_from_providers_and_a_declaration_from_publishers()
    {
        var provider = Evaluator.Evaluate(Book, Context(markets: ["china"], modalities: ["text"]));
        Assert.Equal(["aigc_metadata", "visible_label", "labelling_terms", "no_mark_removal"], provider.Required);
        Assert.All(provider.Findings, f => Assert.Equal(new DateOnly(2025, 9, 1), f.AppliesFrom));

        var publisher = Evaluator.Evaluate(Book, Context(markets: ["china"], roles: ["deployer"]));
        Assert.NotNull(Rule(publisher, "cn-declare-when-publishing"));
        Assert.Null(Rule(publisher, "cn-aigc-metadata"));
    }

    [Theory]
    [InlineData(false, new[] { "kr-31-1-prior-notice", "kr-31-2-output-label" })]
    [InlineData(true, new[] { "kr-31-1-prior-notice", "kr-31-2-output-label", "kr-31-3-realistic" })]
    public void Korea_asks_every_provider_for_notice_and_a_label_and_realistic_output_for_more(bool realistic, string[] rules)
    {
        var evaluation = Evaluator.Evaluate(Book, Context(markets: ["south-korea"], realistic: realistic));

        Assert.Equal(rules, evaluation.Findings.Select(f => f.Rule.Id).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("2026-01-21", FindingStatus.Upcoming)]
    [InlineData("2026-01-22", FindingStatus.InForce)]
    public void Korea_applies_from_22_january_2026(string date, FindingStatus status)
    {
        Assert.Equal(status, Rule(Evaluator.Evaluate(Book, Context(markets: ["south-korea"], date: date)), "kr-31-1-prior-notice")!.Status);
    }

    [Theory]
    [InlineData(true, new[] { "image" }, 5)]
    [InlineData(false, new[] { "image" }, 0)]
    [InlineData(true, new[] { "text" }, 0)]
    public void India_reaches_realistic_audio_images_and_video_only(bool realistic, string[] modalities, int rules)
    {
        var evaluation = Evaluator.Evaluate(Book, Context(markets: ["india"], modalities: modalities, realistic: realistic));

        Assert.Equal(rules, evaluation.Findings.Count);
        if (rules > 0)
        {
            Assert.Equal(
                ["provenance_metadata", "visible_label", "no_mark_removal", "unlawful_content_safeguards", "user_reminders"],
                evaluation.Required);
        }
    }

    [Theory]
    [InlineData(true, new[] { "video" }, true)]
    [InlineData(true, new[] { "audio" }, false)]
    [InlineData(false, new[] { "video" }, false)]
    public void New_york_wants_a_disclosure_on_visual_ads_with_a_synthetic_performer(bool ad, string[] modalities, bool applies)
    {
        var evaluation = Evaluator.Evaluate(Book, Context(markets: ["new-york"], roles: ["deployer"], modalities: modalities, syntheticPerformerAd: ad));

        Assert.Equal(applies, Rule(evaluation, "ny-synthetic-performer-ad") is not null);
    }

    [Fact]
    public void Nothing_applies_outside_the_markets_and_platforms_covered()
    {
        var evaluation = Evaluator.Evaluate(Book, Context(markets: [], platforms: []));

        Assert.Empty(evaluation.Findings);
        Assert.Empty(evaluation.Required);
    }

    [Theory]
    [InlineData("""{ "fact": "planet", "is": true }""", "planet")]
    [InlineData("""{ "fact": "markets", "any_of": ["mars"] }""", "markets")]
    [InlineData("""{ "fact": "realistic", "any_of": ["yes"] }""", "realistic")]
    public void A_pack_with_a_malformed_condition_does_not_load(string condition, string fact)
    {
        var pack = RuleBook.Parse(PackWith(condition: condition));

        var problem = Assert.Throws<InvalidDataException>(() => new RuleBook([pack]));
        Assert.Contains(fact, problem.Message);
    }

    [Fact]
    public void A_pack_citing_a_missing_source_or_asking_for_something_unknown_does_not_load()
    {
        Assert.Throws<InvalidDataException>(() => new RuleBook([RuleBook.Parse(PackWith(sourceId: "nowhere"))]));
        Assert.Throws<InvalidDataException>(() => new RuleBook([RuleBook.Parse(PackWith(requires: "a_feeling"))]));
    }

    private static string PackWith(
        string condition = """{ "fact": "markets", "any_of": ["eu"] }""",
        string sourceId = "s",
        string requires = "watermark") => $$"""
        {
          "pack": "test", "version": "1", "title": "Test", "kind": "law", "changelog": "",
          "sources": [ { "id": "s", "title": "S", "url": "https://example.org", "read": "2026-10-12" } ],
          "rules": [ {
            "id": "r", "title": "R", "summary": "S", "strength": "law",
            "when": [ {{condition}} ],
            "effective_from": "2026-08-02", "grace": null, "requires": ["{{requires}}"],
            "quote": null, "source_ids": ["{{sourceId}}"], "page": "/law/x", "unverified": null
          } ]
        }
        """;
}
