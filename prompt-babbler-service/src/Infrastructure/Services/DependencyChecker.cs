using System.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class DependencyChecker(
    CosmosClient cosmosClient,
    IChatClient? chatClient,
    ILogger<DependencyChecker> logger) : IDependencyChecker
{
    private const int MaxErrorLength = 500;

    public async Task<DependencyStatus> CheckManagedIdentityAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]),
                cancellationToken);
            sw.Stop();

            logger.LogInformation("Managed identity check succeeded in {DurationMs}ms", sw.ElapsedMilliseconds);
            return new DependencyStatus
            {
                Status = DependencyHealth.Healthy,
                Message = "Managed identity token acquired successfully",
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (CredentialUnavailableException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Managed identity not available ({DurationMs}ms)", sw.ElapsedMilliseconds);
            return new DependencyStatus
            {
                Status = DependencyHealth.Unhealthy,
                Message = "Managed identity credential not available",
                Error = Truncate($"{ex.GetType().Name}: {ex.Message}"),
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (AuthenticationFailedException ex)
        {
            sw.Stop();
            logger.LogWarning(ex, "Managed identity authentication failed ({DurationMs}ms)", sw.ElapsedMilliseconds);
            return new DependencyStatus
            {
                Status = DependencyHealth.Unhealthy,
                Message = "Managed identity authentication failed",
                Error = Truncate($"{ex.GetType().Name}: {ex.Message}"),
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
    }

    public async Task<DependencyStatus> CheckCosmosDbAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await cosmosClient.ReadAccountAsync();
            sw.Stop();

            logger.LogInformation("Cosmos DB check succeeded in {DurationMs}ms", sw.ElapsedMilliseconds);
            return new DependencyStatus
            {
                Status = DependencyHealth.Healthy,
                Message = "Cosmos DB account is reachable",
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            // Unwrap authentication-related exceptions for clearer diagnostics.
            var rootCause = ex;
            if (ex.InnerException is AuthenticationFailedException or CredentialUnavailableException)
            {
                rootCause = ex.InnerException;
            }

            logger.LogWarning(ex, "Cosmos DB check failed ({DurationMs}ms)", sw.ElapsedMilliseconds);
            return new DependencyStatus
            {
                Status = DependencyHealth.Unhealthy,
                Message = "Cosmos DB is unreachable",
                Error = Truncate($"{rootCause.GetType().Name}: {rootCause.Message}"),
                DurationMs = sw.ElapsedMilliseconds,
            };
        }
    }

    public DependencyStatus CheckAiFoundry()
    {
        if (chatClient is null)
        {
            logger.LogInformation("AI Foundry check: IChatClient not registered (optional dependency)");
            return new DependencyStatus
            {
                Status = DependencyHealth.Degraded,
                Message = "IChatClient not registered — AI features unavailable",
                DurationMs = 0,
            };
        }

        var metadata = chatClient.GetService<ChatClientMetadata>();
        var providerUri = metadata?.ProviderUri?.ToString() ?? "unknown";

        logger.LogInformation("AI Foundry check: IChatClient registered (provider: {ProviderUri})", providerUri);
        return new DependencyStatus
        {
            Status = DependencyHealth.Healthy,
            Message = $"IChatClient registered (provider: {providerUri})",
            DurationMs = 0,
        };
    }

    private static string Truncate(string value) =>
        value.Length <= MaxErrorLength ? value : string.Concat(value.AsSpan(0, MaxErrorLength), "...");
}
