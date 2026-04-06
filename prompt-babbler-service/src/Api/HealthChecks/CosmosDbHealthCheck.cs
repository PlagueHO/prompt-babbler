using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PromptBabbler.Api.HealthChecks;

public sealed class CosmosDbHealthCheck(CosmosClient cosmosClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await cosmosClient.ReadAccountAsync();
            return HealthCheckResult.Healthy("Cosmos DB account is reachable");
        }
        catch (Exception ex)
        {
            // Unwrap authentication-related exceptions for clearer diagnostics.
            var rootCause = ex;
            if (ex.InnerException is AuthenticationFailedException or CredentialUnavailableException)
            {
                rootCause = ex.InnerException;
            }

            var message = $"{rootCause.GetType().Name}: {rootCause.Message}";
            return HealthCheckResult.Unhealthy(
                message.Length > 500 ? message[..500] : message,
                ex);
        }
    }
}
