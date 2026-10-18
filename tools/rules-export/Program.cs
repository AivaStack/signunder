// Writes content/rules/ for signunder.com: every rule pack as JSON, and the answers to
// the common situations in content/rules/scenarios.json (`make rules`).
using Signunder.Rules;

var dir = args.Length > 0 ? args[0] : Path.Combine("content", "rules");
var book = RuleBook.Embedded();
var scenarios = Export.ParseScenarios(File.ReadAllText(Path.Combine(dir, "scenarios.json")));
foreach (var (path, text) in Export.Files(book, scenarios))
{
    var file = Path.Combine(dir, path);
    Directory.CreateDirectory(Path.GetDirectoryName(file)!);
    File.WriteAllText(file, text);
    Console.WriteLine($"wrote {file}");
}
