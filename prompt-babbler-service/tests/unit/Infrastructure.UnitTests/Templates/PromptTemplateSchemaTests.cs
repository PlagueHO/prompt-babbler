using System.Text.Json;
using FluentAssertions;
using Json.Schema;

namespace PromptBabbler.Infrastructure.UnitTests.Templates;

[TestClass]
[TestCategory("Unit")]
public sealed class PromptTemplateSchemaTests
{
    private static readonly string TemplatesDirectory = FindTemplatesDirectory();
    private static readonly JsonSchema Schema = LoadSchema();

    private static string FindTemplatesDirectory()
    {
        // Walk up from the test output directory to find the repo root,
        // then navigate to the templates folder.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PromptBabbler.slnx")))
        {
            dir = dir.Parent;
        }

        var templatesPath = dir is not null
            ? Path.Combine(dir.FullName, "src", "Infrastructure", "Templates")
            : throw new InvalidOperationException("Could not find the solution root directory.");

        Directory.Exists(templatesPath).Should().BeTrue($"Templates directory should exist at {templatesPath}");
        return templatesPath;
    }

    private static JsonSchema LoadSchema()
    {
        var schemaPath = Path.Combine(TemplatesDirectory, "prompt-template.schema.json");
        File.Exists(schemaPath).Should().BeTrue($"Schema file should exist at {schemaPath}");

        var schemaText = File.ReadAllText(schemaPath);
        return JsonSchema.FromText(schemaText);
    }

    private static IEnumerable<string[]> GetTemplateFiles()
    {
        return Directory.GetFiles(TemplatesDirectory, "builtin-*.json")
            .Select(f => new[] { Path.GetFileName(f) });
    }

    [TestMethod]
    public void TemplatesDirectory_ContainsAtLeastOneTemplate()
    {
        var templateFiles = Directory.GetFiles(TemplatesDirectory, "builtin-*.json");
        templateFiles.Should().NotBeEmpty("at least one built-in template file should exist");
    }

    [TestMethod]
    [DynamicData(nameof(GetTemplateFiles))]
    public void TemplateFile_ConformsToSchema(string fileName)
    {
        var filePath = Path.Combine(TemplatesDirectory, fileName);
        var jsonText = File.ReadAllText(filePath);
        var jsonDoc = JsonDocument.Parse(jsonText);

        var result = Schema.Evaluate(jsonDoc.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
        });

        result.IsValid.Should().BeTrue(
            $"{fileName} should conform to the schema but had errors: {FormatErrors(result)}");
    }

    [TestMethod]
    [DynamicData(nameof(GetTemplateFiles))]
    public void TemplateFile_HasSchemaVersion(string fileName)
    {
        var filePath = Path.Combine(TemplatesDirectory, fileName);
        var jsonText = File.ReadAllText(filePath);
        var doc = JsonDocument.Parse(jsonText);

        doc.RootElement.TryGetProperty("schemaVersion", out var version).Should().BeTrue(
            $"{fileName} must contain a 'schemaVersion' property");

        version.GetString().Should().MatchRegex(@"^\d+\.\d+$",
            $"{fileName} schemaVersion must be in 'major.minor' format");
    }

    private static string FormatErrors(EvaluationResults results)
    {
        if (results.Details is null)
        {
            return "No details available";
        }

        var errors = results.Details
            .Where(d => d.Errors is not null)
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}"));

        return string.Join("; ", errors);
    }
}
