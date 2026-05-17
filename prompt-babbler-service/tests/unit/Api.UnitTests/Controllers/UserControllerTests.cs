using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Api.Controllers;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.UnitTests.Controllers;

[TestClass]
[TestCategory("Unit")]
public sealed class UserControllerTests
{
    private const string TestUserId = "00000000-0000-0000-0000-000000000001";

    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly ILogger<UserController> _logger = Substitute.For<ILogger<UserController>>();
    private readonly UserController _controller;

    public UserControllerTests()
    {
        _controller = new UserController(_userService, _logger)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", TestUserId),
                        new Claim("name", "Test User"),
                        new Claim("preferred_username", "test@contoso.com"),
                    ], "TestAuth")),
                },
            },
        };
    }

    [TestMethod]
    public async Task GetCurrentUser_WhenProfileExists_ReturnsOkResponse()
    {
        var now = DateTimeOffset.UtcNow;
        _userService.GetOrCreateAsync(TestUserId, "Test User", "test@contoso.com", Arg.Any<CancellationToken>())
            .Returns(new UserProfile
            {
                Id = "profile-1",
                UserId = TestUserId,
                DisplayName = "Test User",
                Email = "test@contoso.com",
                Settings = new UserSettings
                {
                    Theme = "system",
                    SpeechLanguage = "en-US",
                },
                CreatedAt = now,
                UpdatedAt = now,
            });

        var result = await _controller.GetCurrentUser(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.Id.Should().Be("profile-1");
        response.DisplayName.Should().Be("Test User");
        response.Email.Should().Be("test@contoso.com");
        response.Settings.Theme.Should().Be("system");
        response.Settings.SpeechLanguage.Should().Be("en-US");

        await _userService.Received(1).GetOrCreateAsync(
            TestUserId,
            "Test User",
            "test@contoso.com",
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateSettings_WhenValidRequest_ReturnsOkAndNormalizesTheme()
    {
        var now = DateTimeOffset.UtcNow;
        _userService.UpdateSettingsAsync(
                TestUserId,
                Arg.Is<UserSettings>(x => x.Theme == "dark" && x.SpeechLanguage == "en-US"),
                Arg.Any<CancellationToken>())
            .Returns(new UserProfile
            {
                Id = "profile-1",
                UserId = TestUserId,
                DisplayName = "Test User",
                Email = "test@contoso.com",
                Settings = new UserSettings
                {
                    Theme = "dark",
                    SpeechLanguage = "en-US",
                },
                CreatedAt = now,
                UpdatedAt = now,
            });

        var result = await _controller.UpdateSettings(
            new UpdateUserSettingsRequest
            {
                Theme = "DARK",
                SpeechLanguage = "en-US",
            },
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<UserProfileResponse>().Subject;
        response.Settings.Theme.Should().Be("dark");

        await _userService.Received(1).UpdateSettingsAsync(
            TestUserId,
            Arg.Is<UserSettings>(x => x.Theme == "dark" && x.SpeechLanguage == "en-US"),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateSettings_WhenThemeInvalid_ReturnsBadRequest()
    {
        var result = await _controller.UpdateSettings(
            new UpdateUserSettingsRequest
            {
                Theme = "blue",
                SpeechLanguage = "en-US",
            },
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("Theme must be one of: light, dark, system.");

        await _userService.DidNotReceive().UpdateSettingsAsync(
            Arg.Any<string>(),
            Arg.Any<UserSettings>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task UpdateSettings_WhenSpeechLanguageTooLong_ReturnsBadRequest()
    {
        var result = await _controller.UpdateSettings(
            new UpdateUserSettingsRequest
            {
                Theme = "light",
                SpeechLanguage = "language-123",
            },
            CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().Be("SpeechLanguage must be at most 10 characters.");

        await _userService.DidNotReceive().UpdateSettingsAsync(
            Arg.Any<string>(),
            Arg.Any<UserSettings>(),
            Arg.Any<CancellationToken>());
    }
}
