using System.Net;
using FluentAssertions;
using PromptBabbler.Api.IntegrationTests.Infrastructure;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class StatusControllerTests
{
    [TestMethod]
    public async Task GetStatus_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ok");
    }

    [TestMethod]
    public async Task GetStatus_DoesNotRequireAuthentication()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
