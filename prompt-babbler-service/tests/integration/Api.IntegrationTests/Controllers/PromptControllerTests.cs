using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PromptBabbler.Api.IntegrationTests.Infrastructure;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class PromptControllerTests
{
    [TestMethod]
    public async Task GeneratePrompt_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { babbleText = "Hello world", templateId = "template-1" };
        var response = await client.PostAsJsonAsync("/api/prompts/generate", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
