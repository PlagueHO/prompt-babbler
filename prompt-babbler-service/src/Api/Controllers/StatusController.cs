using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api")]
public sealed class StatusController(
    IDependencyChecker dependencyChecker,
    IHostEnvironment environment,
    ILogger<StatusController> logger) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatusAsync(CancellationToken cancellationToken)
    {
        var managedIdentity = await dependencyChecker.CheckManagedIdentityAsync(cancellationToken);
        var cosmosDb = await dependencyChecker.CheckCosmosDbAsync(cancellationToken);
        var aiFoundry = dependencyChecker.CheckAiFoundry();

        // Overall is Healthy only if both ManagedIdentity and CosmosDb are Healthy.
        // AI Foundry Degraded is acceptable (optional dependency).
        var overall = managedIdentity.Status == DependencyHealth.Healthy
            && cosmosDb.Status == DependencyHealth.Healthy
            ? DependencyHealth.Healthy
            : DependencyHealth.Unhealthy;

        var response = new StatusResponse
        {
            Timestamp = DateTimeOffset.UtcNow,
            Environment = environment.EnvironmentName,
            Overall = overall,
            ManagedIdentity = managedIdentity,
            CosmosDb = cosmosDb,
            AiFoundry = aiFoundry,
        };

        logger.LogInformation(
            "Status check: Overall={Overall}, ManagedIdentity={MiStatus}, CosmosDb={CosmosStatus}, AiFoundry={AiStatus}",
            overall, managedIdentity.Status, cosmosDb.Status, aiFoundry.Status);

        return overall == DependencyHealth.Healthy
            ? Ok(response)
            : StatusCode(503, response);
    }
}
