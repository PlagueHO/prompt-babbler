using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class BuiltInTemplateSeedingService : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Delays in seconds between successive retry attempts when Cosmos DB returns 503.
    // Total back-off budget: 2 + 4 + 8 + 16 + 30 = 60 seconds.
    private static readonly int[] RetryDelaySeconds = [2, 4, 8, 16, 30];

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
            await UpsertWithRetryAsync(template, cancellationToken);
            _logger.LogInformation("Seeded built-in template: {TemplateName}", template.Name);
        }

        _logger.LogInformation("Built-in prompt template seeding complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task UpsertWithRetryAsync(PromptTemplate template, CancellationToken cancellationToken)
    {
        // The Cosmos DB emulator (pgcosmos) can return 503 while its internal
        // extension is still initialising, even after Aspire reports the resource
        // as healthy. We retry with exponential back-off so a slow emulator start
        // does not crash the API at startup.
        for (var attempt = 0; attempt <= RetryDelaySeconds.Length; attempt++)
        {
            try
            {
                await _repository.UpsertAsync(template, cancellationToken);
                return;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < RetryDelaySeconds.Length)
            {
                var delaySeconds = RetryDelaySeconds[attempt];
                _logger.LogWarning(
                    ex,
                    "Cosmos DB unavailable during template seeding (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}s.",
                    attempt + 1, RetryDelaySeconds.Length + 1, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }
    }

    public static IReadOnlyList<PromptTemplate> GetBuiltInTemplates()
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
