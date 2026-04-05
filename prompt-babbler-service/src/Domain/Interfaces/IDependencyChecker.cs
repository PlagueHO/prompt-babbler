using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface IDependencyChecker
{
    Task<DependencyStatus> CheckManagedIdentityAsync(CancellationToken cancellationToken = default);

    Task<DependencyStatus> CheckCosmosDbAsync(CancellationToken cancellationToken = default);

    DependencyStatus CheckAiFoundry();
}
