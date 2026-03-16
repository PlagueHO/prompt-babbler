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
public sealed class BabbleControllerTests
{
    [TestMethod]
    public async Task GetBabbles_WithAuth_ReturnsOk()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var babbleService = factory.Services.GetRequiredService<IBabbleService>();
        babbleService.GetByUserAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<Babble> { BabbleFixtures.CreateBabble() }.AsReadOnly(), (string?)null));

        var response = await client.GetAsync("/api/babbles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task GetBabbles_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/babbles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateBabble_ValidRequest_Returns201()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var babbleService = factory.Services.GetRequiredService<IBabbleService>();
        babbleService.CreateAsync(Arg.Any<Babble>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Babble>());

        var request = new { title = "Test Babble", text = "This is a test babble." };
        var response = await client.PostAsJsonAsync("/api/babbles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [TestMethod]
    public async Task CreateBabble_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { title = "Test Babble", text = "This is a test babble." };
        var response = await client.PostAsJsonAsync("/api/babbles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task CreateBabble_EmptyTitle_Returns400()
    {
        await using var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient();

        var request = new { title = "", text = "Valid text." };
        var response = await client.PostAsJsonAsync("/api/babbles", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task DeleteBabble_WithoutAuth_Returns401()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/babbles/test-id");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
