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
public sealed class PromptTemplateControllerTests
{
    [TestMethod]
    public async Task GetTemplates_WithAuth_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var templateService = factory.Services.GetRequiredService<IPromptTemplateService>();
        templateService.GetTemplatesAsync(Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<PromptTemplate> { PromptTemplateFixtures.CreateUserTemplate() });

        var response = await client.GetAsync("/api/templates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetTemplates_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/templates");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateTemplate_ValidRequest_Returns201()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var templateService = factory.Services.GetRequiredService<IPromptTemplateService>();
        templateService.CreateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PromptTemplate>());

        var request = new { name = "Test", description = "Test description", systemPrompt = "Be helpful." };
        var response = await client.PostAsJsonAsync("/api/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateTemplate_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { name = "Test", description = "Test description", systemPrompt = "Be helpful." };
        var response = await client.PostAsJsonAsync("/api/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateTemplate_EmptyName_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { name = "", description = "Test description", systemPrompt = "Be helpful." };
        var response = await client.PostAsJsonAsync("/api/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task DeleteTemplate_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/templates/some-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
