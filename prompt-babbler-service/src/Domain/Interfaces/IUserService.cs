using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IUserService
{
    Task<UserProfile> GetOrCreateAsync(string userId, string? displayName = null, string? email = null, CancellationToken cancellationToken = default);

    Task<UserProfile> UpdateSettingsAsync(string userId, UserSettings settings, CancellationToken cancellationToken = default);

    Task<UserProfile> UpdateProfileAsync(string userId, string? displayName, string? email, CancellationToken cancellationToken = default);
}
