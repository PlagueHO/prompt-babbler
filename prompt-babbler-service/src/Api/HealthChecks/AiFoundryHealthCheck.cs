using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PromptBabbler.Api.HealthChecks;

public sealed class AiFoundryHealthCheck(IChatClient? chatClient = null) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (chatClient is null)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "IChatClient not registered — AI features unavailable"));
        }

        var metadata = chatClient.GetService<ChatClientMetadata>();
        var description = $"endpoint={metadata?.ProviderUri}, model={metadata?.DefaultModelId}";

        return Task.FromResult(HealthCheckResult.Healthy($"Configured: {description}"));
    }
}
