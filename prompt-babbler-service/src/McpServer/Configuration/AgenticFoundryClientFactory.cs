using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Hosting;

namespace PromptBabbler.McpServer.Configuration;

public sealed class AgenticFoundryClientFactory(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment) : IAgenticFoundryClientFactory
{
    private const string MissingEndpointMessage =
        "Agentic Foundry project endpoint is not configured. Configure Agentic:FoundryProjectEndpoint, AZURE_AI_PROJECT_ENDPOINT, or ConnectionStrings:ai-foundry.";

    public string? ResolveProjectEndpoint()
    {
        var configuredEndpoint = configuration["Agentic:FoundryProjectEndpoint"];
        if (!string.IsNullOrWhiteSpace(configuredEndpoint))
        {
            return configuredEndpoint.Trim().TrimEnd('/');
        }

        var environmentEndpoint = Environment.GetEnvironmentVariable("AZURE_AI_PROJECT_ENDPOINT");
        if (!string.IsNullOrWhiteSpace(environmentEndpoint))
        {
            return environmentEndpoint.Trim().TrimEnd('/');
        }

        var aiFoundryConnectionString = configuration.GetConnectionString("ai-foundry") ?? string.Empty;
        foreach (var part in aiFoundryConnectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["Endpoint=".Length..].Trim().TrimEnd('/');
            }
        }

        if (aiFoundryConnectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return aiFoundryConnectionString.Split(';')[0].Trim().TrimEnd('/');
        }

        return null;
    }

    public AIProjectClient CreateClient()
    {
        var endpoint = ResolveProjectEndpoint();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException(MissingEndpointMessage);
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var projectEndpoint))
        {
            throw new InvalidOperationException($"Agentic Foundry project endpoint '{endpoint}' is not a valid absolute URI.");
        }

        return new AIProjectClient(projectEndpoint, CreateTokenCredential());
    }

    private TokenCredential CreateTokenCredential()
    {
        var tenantId = configuration["Azure:TenantId"];

        if (hostEnvironment.IsDevelopment())
        {
            return !string.IsNullOrEmpty(tenantId)
                ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    TenantId = tenantId,
                    ExcludeManagedIdentityCredential = true,
                })
                : new DefaultAzureCredential();
        }

        return new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
    }
}