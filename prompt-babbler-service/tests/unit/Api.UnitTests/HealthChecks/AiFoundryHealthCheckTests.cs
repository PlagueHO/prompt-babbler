using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using PromptBabbler.Api.HealthChecks;

namespace PromptBabbler.Api.UnitTests.HealthChecks;

[TestClass]
[TestCategory("Unit")]
public sealed class AiFoundryHealthCheckTests
{
    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientNull_ReturnsDegraded()
    {
        var healthCheck = new AiFoundryHealthCheck(chatClient: null);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("IChatClient not registered");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientUsesProjectEndpoint_ReturnsHealthy()
    {
        var chatClient = Substitute.For<IChatClient>();
        var metadata = new ChatClientMetadata(
            providerName: "test",
            providerUri: new Uri("https://ai.example.com/api/projects/promptbabbler"),
            defaultModelId: "gpt-4o");
        chatClient.GetService<ChatClientMetadata>().Returns(metadata);

        var healthCheck = new AiFoundryHealthCheck(chatClient);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("/api/projects/promptbabbler");
    }

    [TestMethod]
    public async Task CheckHealthAsync_WhenChatClientHasNoMetadata_ReturnsHealthy()
    {
        var chatClient = Substitute.For<IChatClient>();
        chatClient.GetService<ChatClientMetadata>().Returns((ChatClientMetadata?)null);

        var healthCheck = new AiFoundryHealthCheck(chatClient);

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("Configured");
    }
}
