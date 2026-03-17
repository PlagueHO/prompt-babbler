using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserProfile> GetOrCreateAsync(
        string userId,
        string? displayName = null,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            // Update cached Entra ID profile info if it has changed.
            if (displayName is not null && (existing.DisplayName != displayName || existing.Email != email))
            {
                var updated = existing with
                {
                    DisplayName = displayName,
                    Email = email,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };
                return await _userRepository.UpsertAsync(updated, cancellationToken);
            }

            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var profile = new UserProfile
        {
            Id = userId,
            UserId = userId,
            DisplayName = displayName,
            Email = email,
            Settings = UserSettings.Default,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _logger.LogInformation("Creating default user profile for user {UserId}", userId);
        return await _userRepository.UpsertAsync(profile, cancellationToken);
    }

    public async Task<UserProfile> UpdateSettingsAsync(
        string userId,
        UserSettings settings,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            // Defensive: auto-create profile if it doesn't exist.
            var now = DateTimeOffset.UtcNow;
            var profile = new UserProfile
            {
                Id = userId,
                UserId = userId,
                Settings = settings,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _logger.LogInformation("Auto-creating user profile during settings update for user {UserId}", userId);
            return await _userRepository.UpsertAsync(profile, cancellationToken);
        }

        var updated = existing with
        {
            Settings = settings,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _logger.LogInformation("Updated settings for user {UserId}", userId);
        return await _userRepository.UpsertAsync(updated, cancellationToken);
    }

    public async Task<UserProfile> UpdateProfileAsync(
        string userId,
        string? displayName,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (existing is null)
        {
            // Defensive: auto-create profile if it doesn't exist.
            var now = DateTimeOffset.UtcNow;
            var profile = new UserProfile
            {
                Id = userId,
                UserId = userId,
                DisplayName = displayName,
                Email = email,
                Settings = UserSettings.Default,
                CreatedAt = now,
                UpdatedAt = now,
            };

            _logger.LogInformation("Auto-creating user profile during profile update for user {UserId}", userId);
            return await _userRepository.UpsertAsync(profile, cancellationToken);
        }

        var updated = existing with
        {
            DisplayName = displayName,
            Email = email,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _logger.LogInformation("Updated profile for user {UserId}", userId);
        return await _userRepository.UpsertAsync(updated, cancellationToken);
    }
}
