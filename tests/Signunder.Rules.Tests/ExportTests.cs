namespace Signunder.Rules.Tests;

public class ExportTests
{
    private static readonly RuleBook Book = RuleBook.Embedded();

    /// <summary>The repository's content/rules folder, found from the test's working directory.</summary>
    private static string RulesDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Signunder.sln")))
        {
            dir = dir.Parent;
        }
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("Signunder.sln not found"), "content", "rules");
    }

    private static Export.Scenarios Scenarios() =>
        Export.ParseScenarios(File.ReadAllText(Path.Combine(RulesDir(), "scenarios.json")));

    [Fact]
    public void Each_situation_still_triggers_the_rules_its_answer_describes()
    {
        var scenarios = Scenarios();
        Assert.NotEmpty(scenarios.Items);
        foreach (var scenario in scenarios.Items)
        {
            var binding = Evaluator.Evaluate(Book, Export.Context(scenario.Context, scenarios.AsOf))
                .Findings.Where(f => f.Binding).Select(f => f.Rule.Id).Order(StringComparer.Ordinal);
            Assert.True(
                binding.SequenceEqual(scenario.Expect.Order(StringComparer.Ordinal)),
                $"{scenario.Slug}: the packs now give [{string.Join(", ", binding)}]; review its answer and expect list.");
        }
    }

    [Fact]
    public void Situations_have_unique_slugs_and_search_length_descriptions()
    {
        var scenarios = Scenarios().Items;
        Assert.Equal(scenarios.Count, scenarios.Select(s => s.Slug).Distinct().Count());
        Assert.All(scenarios, s =>
        {
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", s.Slug);
            Assert.True(s.Description.Length <= 160, $"{s.Slug}: description is {s.Description.Length} characters.");
            Assert.True(s.Question.Length <= 80, $"{s.Slug}: question is {s.Question.Length} characters.");
        });
    }

    [Fact]
    public void The_published_files_are_current()
    {
        foreach (var (path, text) in Export.Files(Book, Scenarios()))
        {
            var file = Path.Combine(RulesDir(), path);
            Assert.True(File.Exists(file) && File.ReadAllText(file) == text, $"content/rules/{path} is out of date: run `make rules`.");
        }
    }

    [Fact]
    public void A_situation_s_checker_link_carries_its_answers_and_date()
    {
        var query = Export.CheckerQuery(
            new Export.ScenarioContext(["image", "video"], ["provider"], ["eu"], null, true, false, true, false, false),
            new DateOnly(2026, 10, 18));

        Assert.Equal("m=image,video&r=provider&w=eu&pre=1&real=1&d=2026-10-18", query);
    }
}
