using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using PromptBabbler.Api.IntegrationTests.Fixtures;
using PromptBabbler.Api.IntegrationTests.Infrastructure;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Constants;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Api.IntegrationTests.Controllers;

[TestClass]
[TestCategory("Integration")]
public sealed class PromptTemplateControllerTests
{
    [TestMethod]
    public async Task CreateTemplate_AnonymousUser_CanListRetrieveAndGenerateWithSavedTemplate()
    {
        await using var anonymousFactory = new AnonymousModeWebApplicationFactory();
        await using var factory = anonymousFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPromptTemplateService>();
                services.AddScoped<IPromptTemplateService, PromptTemplateService>();
            }));
        var client = factory.CreateClient();
        var stored = new List<PromptTemplate>();
        var repository = factory.Services.GetRequiredService<IPromptTemplateRepository>();
        repository.CreateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var template = call.Arg<PromptTemplate>();
                stored.Add(template);
                return template;
            });
        repository.GetByIdAsync(UserIds.Anonymous, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => stored.SingleOrDefault(template => template.Id == call.ArgAt<string>(1)));
        repository.ListTemplatesAsync(
                UserIds.Anonymous,
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => ((IReadOnlyList<PromptTemplate>)stored.ToArray(), null));
        factory.Services.GetRequiredService<ITemplateValidationService>()
            .ValidateTemplateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(TemplateValidationResult.Success());

        var response = await client.PostAsJsonAsync("/api/templates", new
        {
            name = "Custom template",
            description = "A custom template",
            instructions = "Summarize the babble.",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PromptTemplateResponse>();
        created.Should().NotBeNull();
        stored.Should().ContainSingle().Which.UserId.Should().Be(UserIds.Anonymous);

        var retrieved = await client.GetFromJsonAsync<PromptTemplateResponse>(response.Headers.Location);
        retrieved.Should().BeEquivalentTo(created);

        var listed = await client.GetFromJsonAsync<PagedResponse<PromptTemplateResponse>>("/api/templates");
        listed!.Items.Should().ContainSingle().Which.Id.Should().Be(created!.Id);

        var babble = new Babble
        {
            Id = "test-babble",
            UserId = UserIds.Anonymous,
            Title = "Test babble",
            Text = "Synthetic test input.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        factory.Services.GetRequiredService<IBabbleService>()
            .GetByIdAsync(UserIds.Anonymous, babble.Id, Arg.Any<CancellationToken>())
            .Returns(babble);
        factory.Services.GetRequiredService<IPromptGenerationService>()
            .GeneratePromptStreamAsync(
                babble.Text,
                Arg.Is<PromptTemplate>(template => template.Id == created!.Id),
                Arg.Any<string?>(),
                Arg.Any<bool?>(),
                Arg.Any<CancellationToken>())
            .Returns(GenerateChunks());
        factory.Services.GetRequiredService<IGeneratedPromptService>()
            .CreateAsync(UserIds.Anonymous, Arg.Any<GeneratedPrompt>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<GeneratedPrompt>());

        var generated = await client.PostAsJsonAsync(
            $"/api/babbles/{babble.Id}/generate", new { templateId = created!.Id });

        generated.StatusCode.Should().Be(HttpStatusCode.OK);
        var stream = await generated.Content.ReadAsStringAsync();
        stream.Should().Contain("\"text\":\"A concise summary.\"");
        stream.Should().Contain("\"promptId\":");
        stream.Should().Contain("data: [DONE]");
    }

    private static async IAsyncEnumerable<string> GenerateChunks()
    {
        yield return "A concise summary.";
        await Task.CompletedTask;
    }

    [TestMethod]
    public async Task GetTemplates_WithAuth_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var templateService = factory.Services.GetRequiredService<IPromptTemplateService>();
        templateService.ListTemplatesAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((new List<PromptTemplate> { PromptTemplateFixtures.CreateUserTemplate() }, null));

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
            .Returns(ci => ci.Arg<PromptTemplate>()!);

        var validationService = factory.Services.GetRequiredService<ITemplateValidationService>();
        validationService.ValidateTemplateAsync(Arg.Any<PromptTemplate>(), Arg.Any<CancellationToken>())
            .Returns(TemplateValidationResult.Success());

        var request = new { name = "Test", description = "Test description", instructions = "Be helpful." };
        var response = await client.PostAsJsonAsync("/api/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateTemplate_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { name = "Test", description = "Test description", instructions = "Be helpful." };
        var response = await client.PostAsJsonAsync("/api/templates", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateTemplate_EmptyName_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { name = "", description = "Test description", instructions = "Be helpful." };
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
