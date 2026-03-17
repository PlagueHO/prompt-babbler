using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using PromptBabbler.Api.Extensions;
using PromptBabbler.Api.Models.Requests;
using PromptBabbler.Api.Models.Responses;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Api.Controllers;

[ApiController]
[Authorize]
[RequiredScope("access_as_user")]
[Route("api/user")]
public sealed class UserController : ControllerBase
{
    private static readonly HashSet<string> AllowedThemes = new(StringComparer.OrdinalIgnoreCase) { "light", "dark", "system" };

    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserIdOrAnonymous();
        var displayName = User.FindFirst("name")?.Value;
        var email = User.FindFirst("preferred_username")?.Value;

        var profile = await _userService.GetOrCreateAsync(userId, displayName, email, cancellationToken);
        _logger.LogInformation("Retrieved user profile for user {UserId}", userId);

        return Ok(ToResponse(profile));
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateUserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ValidateSettingsRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var userId = User.GetUserIdOrAnonymous();
        var settings = new UserSettings
        {
            Theme = request.Theme.ToLowerInvariant(),
            SpeechLanguage = request.SpeechLanguage,
        };

        var profile = await _userService.UpdateSettingsAsync(userId, settings, cancellationToken);
        _logger.LogInformation("Updated settings for user {UserId}", userId);

        return Ok(ToResponse(profile));
    }

    private static bool ValidateSettingsRequest(UpdateUserSettingsRequest request, out string? error)
    {
        if (string.IsNullOrWhiteSpace(request.Theme) || !AllowedThemes.Contains(request.Theme))
        {
            error = "Theme must be one of: light, dark, system.";
            return false;
        }

        if (request.SpeechLanguage.Length > 10)
        {
            error = "SpeechLanguage must be at most 10 characters.";
            return false;
        }

        error = null;
        return true;
    }

    private static UserProfileResponse ToResponse(UserProfile profile)
    {
        return new UserProfileResponse
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            Settings = new UserSettingsResponse
            {
                Theme = profile.Settings.Theme,
                SpeechLanguage = profile.Settings.SpeechLanguage,
            },
            CreatedAt = profile.CreatedAt.ToString("o"),
            UpdatedAt = profile.UpdatedAt.ToString("o"),
        };
    }
}
