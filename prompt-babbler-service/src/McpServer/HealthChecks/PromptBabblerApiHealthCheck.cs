using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PromptBabbler.McpServer.HealthChecks;

public sealed class PromptBabblerApiHealthCheck(HttpClient httpClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/alive", cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Prompt Babbler API is reachable")
                : HealthCheckResult.Unhealthy($"Prompt Babbler API returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Prompt Babbler API is unreachable", ex);
        }
    }
}
