using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;
using PromptBabbler.Infrastructure.Services;

namespace PromptBabbler.Infrastructure.UnitTests.Services;

[TestClass]
[TestCategory("Unit")]
public sealed class UserServiceTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ILogger<UserService> _logger = Substitute.For<ILogger<UserService>>();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(_userRepository, _logger);
    }

    private static UserProfile CreateProfile(
        string userId = "test-user-id",
        string displayName = "Test User",
        string email = "test@example.com",
        string theme = "system",
        string speechLanguage = "") => new()
        {
            Id = userId,
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            Settings = new UserSettings { Theme = theme, SpeechLanguage = speechLanguage },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    // ---- GetOrCreateAsync ----

    [TestMethod]
    public async Task GetOrCreateAsync_ReturnsExistingProfile_WhenExists()
    {
        var existing = CreateProfile();
        _userRepository.GetByIdAsync("test-user-id", Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _service.GetOrCreateAsync("test-user-id", "Test User", "test@example.com");

        result.Should().Be(existing);
    }

    [TestMethod]
    public async Task GetOrCreateAsync_CreatesDefaultProfile_WhenNotExists()
    {
        _userRepository.GetByIdAsync("new-user", Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var result = await _service.GetOrCreateAsync("new-user", "New User", "new@example.com");

        result.UserId.Should().Be("new-user");
        result.DisplayName.Should().Be("New User");
        result.Email.Should().Be("new@example.com");
        result.Settings.Theme.Should().Be("system");
        result.Settings.SpeechLanguage.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetOrCreateAsync_UpdatesProfileInfo_WhenDisplayNameChanged()
    {
        var existing = CreateProfile(displayName: "Old Name");
        _userRepository.GetByIdAsync("test-user-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var result = await _service.GetOrCreateAsync("test-user-id", "New Name", "new@example.com");

        result.DisplayName.Should().Be("New Name");
        result.Email.Should().Be("new@example.com");
    }

    // ---- UpdateSettingsAsync ----

    [TestMethod]
    public async Task UpdateSettingsAsync_UpdatesAndReturns()
    {
        var existing = CreateProfile();
        _userRepository.GetByIdAsync("test-user-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var newSettings = new UserSettings { Theme = "dark", SpeechLanguage = "en" };
        var result = await _service.UpdateSettingsAsync("test-user-id", newSettings);

        result.Settings.Theme.Should().Be("dark");
        result.Settings.SpeechLanguage.Should().Be("en");
    }

    [TestMethod]
    public async Task UpdateSettingsAsync_CreatesProfile_WhenNotExists()
    {
        _userRepository.GetByIdAsync("new-user", Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var settings = new UserSettings { Theme = "light", SpeechLanguage = "fr" };
        var result = await _service.UpdateSettingsAsync("new-user", settings);

        result.UserId.Should().Be("new-user");
        result.Settings.Theme.Should().Be("light");
        result.Settings.SpeechLanguage.Should().Be("fr");
    }

    // ---- UpdateProfileAsync ----

    [TestMethod]
    public async Task UpdateProfileAsync_UpdatesDisplayNameAndEmail()
    {
        var existing = CreateProfile();
        _userRepository.GetByIdAsync("test-user-id", Arg.Any<CancellationToken>())
            .Returns(existing);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var result = await _service.UpdateProfileAsync("test-user-id", "Updated Name", "updated@example.com");

        result.DisplayName.Should().Be("Updated Name");
        result.Email.Should().Be("updated@example.com");
    }

    [TestMethod]
    public async Task UpdateProfileAsync_CreatesProfile_WhenNotExists()
    {
        _userRepository.GetByIdAsync("new-user", Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);
        _userRepository.UpsertAsync(Arg.Any<UserProfile>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<UserProfile>());

        var result = await _service.UpdateProfileAsync("new-user", "New User", "new@example.com");

        result.UserId.Should().Be("new-user");
        result.DisplayName.Should().Be("New User");
        result.Settings.Theme.Should().Be("system");
    }
}
