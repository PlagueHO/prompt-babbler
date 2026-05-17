using Azure;
using Azure.Core;
using Azure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PromptBabbler.Api.HealthChecks;

namespace PromptBabbler.Api.UnitTests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class ManagedIdentityHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenTokenAcquired_ReturnsHealthy()
    {
        var healthCheck = new ManagedIdentityHealthCheck(new SuccessTokenCredential());

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Managed identity token acquired successfully");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenCredentialUnavailable_ReturnsUnhealthy()
    {
        var healthCheck = new ManagedIdentityHealthCheck(
            new ThrowingTokenCredential(new CredentialUnavailableException("No managed identity endpoint.")));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Managed identity credential not available");
        result.Exception.Should().BeOfType<CredentialUnavailableException>();
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenAuthenticationFails_ReturnsUnhealthy()
    {
        var healthCheck = new ManagedIdentityHealthCheck(
            new ThrowingTokenCredential(new AuthenticationFailedException("Auth failed.")));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Managed identity authentication failed");
        result.Exception.Should().BeOfType<AuthenticationFailedException>();
    }

    private sealed class SuccessTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken("token", DateTimeOffset.UtcNow.AddMinutes(5));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new AccessToken("token", DateTimeOffset.UtcNow.AddMinutes(5)));
        }
    }

    private sealed class ThrowingTokenCredential(Exception exception) : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            throw exception;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }
}
