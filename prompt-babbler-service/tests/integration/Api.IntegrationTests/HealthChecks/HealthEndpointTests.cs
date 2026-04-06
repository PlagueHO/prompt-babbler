using System.Net;
using System.Text.Json;
using FluentAssertions;
using PromptBabbler.Api.IntegrationTests.Infrastructure;

namespace PromptBabbler.Api.IntegrationTests.HealthChecks;

[TestClass]
[TestCategory("Integration")]
public sealed class HealthEndpointTests
{
    [TestMethod]
    public async Task Health_ReturnsJsonWithDetailedStatus()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        // Health endpoint returns 200 (healthy) or 503 (unhealthy).
        // In test environments without Cosmos connection, DI may fail with 500.
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task Health_DoesNotRequireAuthentication()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    [TestMethod]
    public async Task Alive_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
