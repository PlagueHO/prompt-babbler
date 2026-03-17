using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserProfile?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<UserProfile> UpsertAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
