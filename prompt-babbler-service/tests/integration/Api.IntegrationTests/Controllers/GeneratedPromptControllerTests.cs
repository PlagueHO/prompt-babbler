using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PromptBabbler.Api.IntegrationTests.Fixtures;
using PromptBabbler.Api.IntegrationTests.Infrastructure;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class GeneratedPromptControllerTests
{
    [TestMethod]
    public async Task GetPrompts_WithAuth_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var promptService = factory.Services.GetRequiredService<IGeneratedPromptService>();
        promptService.GetByBabbleAsync(Arg.Any<string>(), "test-babble-id", Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<GeneratedPrompt> { GeneratedPromptFixtures.CreateGeneratedPrompt() }.AsReadOnly(), (string?)null));

        var response = await client.GetAsync("/api/babbles/test-babble-id/prompts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetPrompts_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/babbles/test-babble-id/prompts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreatePrompt_ValidRequest_Returns201()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var promptService = factory.Services.GetRequiredService<IGeneratedPromptService>();
        promptService.CreateAsync(Arg.Any<string>(), Arg.Any<GeneratedPrompt>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<GeneratedPrompt>());

        var request = new { templateId = "template-1", templateName = "Test Template", promptText = "Generated prompt text." };
        var response = await client.PostAsJsonAsync("/api/babbles/test-babble-id/prompts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreatePrompt_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { templateId = "template-1", templateName = "Test Template", promptText = "Generated prompt text." };
        var response = await client.PostAsJsonAsync("/api/babbles/test-babble-id/prompts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task DeletePrompt_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/babbles/test-babble-id/prompts/test-prompt-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
