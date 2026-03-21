using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class BuiltInTemplateSeedingService : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPromptTemplateRepository _repository;
    private readonly ILogger<BuiltInTemplateSeedingService> _logger;

    public BuiltInTemplateSeedingService(
        IPromptTemplateRepository repository,
        ILogger<BuiltInTemplateSeedingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Seeding built-in prompt templates...");

        foreach (var template in GetBuiltInTemplates())
        {
            await _repository.UpsertAsync(template, cancellationToken);
            _logger.LogInformation("Seeded built-in template: {TemplateName}", template.Name);
        }

        _logger.LogInformation("Built-in prompt template seeding complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static IReadOnlyList<PromptTemplate> GetBuiltInTemplates()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = typeof(BuiltInTemplateSeedingService).Namespace!.Replace(".Services", ".Templates.");
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".json", StringComparison.Ordinal))
            .Order();

        var now = DateTimeOffset.UtcNow;
        var templates = new List<PromptTemplate>();

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");

            var definition = JsonSerializer.Deserialize<BuiltInTemplateDefinition>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to deserialize template from '{resourceName}'.");

            templates.Add(new PromptTemplate
            {
                Id = definition.Id,
                UserId = CosmosPromptTemplateRepository.BuiltInUserId,
                Name = definition.Name,
                Description = definition.Description,
                Instructions = definition.Instructions,
                OutputDescription = definition.OutputDescription,
                OutputTemplate = definition.OutputTemplate,
                Examples = definition.Examples,
                Guardrails = definition.Guardrails,
                DefaultOutputFormat = definition.DefaultOutputFormat,
                DefaultAllowEmojis = definition.DefaultAllowEmojis,
                Tags = definition.Tags,
                IsBuiltIn = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        return templates;
    }

    internal sealed record BuiltInTemplateDefinition
    {
        public required string SchemaVersion { get; init; }
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string Instructions { get; init; }
        public string? OutputDescription { get; init; }
        public string? OutputTemplate { get; init; }
        public IReadOnlyList<PromptExample>? Examples { get; init; }
        public IReadOnlyList<string>? Guardrails { get; init; }
        public string? DefaultOutputFormat { get; init; }
        public bool? DefaultAllowEmojis { get; init; }
        public IReadOnlyList<string>? Tags { get; init; }
    }
}
