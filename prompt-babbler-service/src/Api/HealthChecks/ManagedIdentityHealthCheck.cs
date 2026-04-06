using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PromptBabbler.Api.HealthChecks;

public sealed class ManagedIdentityHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
            await credential.GetTokenAsync(
                new TokenRequestContext(["https://management.azure.com/.default"]),
                cancellationToken);

            return HealthCheckResult.Healthy("Managed identity token acquired successfully");
        }
        catch (CredentialUnavailableException ex)
        {
            return HealthCheckResult.Unhealthy("Managed identity credential not available", ex);
        }
        catch (AuthenticationFailedException ex)
        {
            return HealthCheckResult.Unhealthy("Managed identity authentication failed", ex);
        }
    }
}
