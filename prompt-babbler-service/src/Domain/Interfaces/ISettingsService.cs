using PromptBabbler.Domain.Models;

namespace PromptBabbler.Domain.Interfaces;

public interface ISettingsService
{
    Task<LlmSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSettingsAsync(LlmSettings settings, CancellationToken cancellationToken = default);
}
