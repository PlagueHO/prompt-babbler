using System.Net;
using FluentAssertions;
using PromptBabbler.Api.IntegrationTests.Infrastructure;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class TranscriptionWebSocketControllerTests
{
    [TestMethod]
    public async Task StreamTranscription_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        // Non-WebSocket request to the WebSocket endpoint without auth should return 401
        var response = await client.GetAsync("/api/transcribe/stream");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
