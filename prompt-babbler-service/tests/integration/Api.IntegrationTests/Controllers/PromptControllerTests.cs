using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PromptBabbler.Api.IntegrationTests.Infrastructure;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class BabbleGenerateIntegrationTests
{
    [TestMethod]
    public async Task GeneratePrompt_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { templateId = "template-1" };
        var response = await client.PostAsJsonAsync("/api/babbles/test-babble-id/generate", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task GenerateTitle_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync("/api/babbles/test-babble-id/generate-title", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
