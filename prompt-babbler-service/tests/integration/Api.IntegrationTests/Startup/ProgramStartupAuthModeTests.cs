using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PromptBabbler.Api.IntegrationTests.Infrastructure;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.IntegrationTests.Startup;

[TestClass]
[TestCategory("Integration")]
public sealed class ProgramStartupAuthModeTests
{
    [TestMethod]
    public async Task UserEndpoint_WhenAuthEnabledAndNoToken_ReturnsUnauthorized()
    {
        await using var factory = new NoAuthWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task UserEndpoint_WhenAuthDisabled_UsesAnonymousIdentityAndReturnsOk()
    {
        await using var factory = new AnonymousModeWebApplicationFactory();

        var userService = factory.Services.GetRequiredService<IUserService>();
        userService.GetOrCreateAsync("_anonymous", null, null, Arg.Any<CancellationToken>())
            .Returns(new UserProfile
            {
                Id = "profile-anon",
                UserId = "_anonymous",
                DisplayName = null,
                Email = null,
                Settings = new UserSettings
                {
                    Theme = "system",
                    SpeechLanguage = "en-US",
                },
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await userService.Received(1).GetOrCreateAsync(
            "_anonymous",
            null,
            null,
            Arg.Any<CancellationToken>());
    }
}
