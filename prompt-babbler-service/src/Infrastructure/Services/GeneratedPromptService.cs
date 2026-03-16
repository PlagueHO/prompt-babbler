using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class GeneratedPromptService : IGeneratedPromptService
{
    private readonly IGeneratedPromptRepository _promptRepository;
    private readonly IBabbleRepository _babbleRepository;
    private readonly ILogger<GeneratedPromptService> _logger;

    public GeneratedPromptService(
        IGeneratedPromptRepository promptRepository,
        IBabbleRepository babbleRepository,
        ILogger<GeneratedPromptService> logger)
    {
        _promptRepository = promptRepository;
        _babbleRepository = babbleRepository;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<GeneratedPrompt> Items, string? ContinuationToken)> GetByBabbleAsync(
        string userId,
        string babbleId,
        string? continuationToken = null,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        await ValidateBabbleOwnershipAsync(userId, babbleId, cancellationToken);
        return await _promptRepository.GetByBabbleAsync(babbleId, continuationToken, pageSize, cancellationToken);
    }

    public async Task<GeneratedPrompt?> GetByIdAsync(
        string userId,
        string babbleId,
        string promptId,
        CancellationToken cancellationToken = default)
    {
        await ValidateBabbleOwnershipAsync(userId, babbleId, cancellationToken);
        return await _promptRepository.GetByIdAsync(babbleId, promptId, cancellationToken);
    }

    public async Task<GeneratedPrompt> CreateAsync(string userId, GeneratedPrompt prompt, CancellationToken cancellationToken = default)
    {
        await ValidateBabbleOwnershipAsync(userId, prompt.BabbleId, cancellationToken);
        return await _promptRepository.CreateAsync(prompt, cancellationToken);
    }

    public async Task DeleteAsync(string userId, string babbleId, string promptId, CancellationToken cancellationToken = default)
    {
        await ValidateBabbleOwnershipAsync(userId, babbleId, cancellationToken);
        await _promptRepository.DeleteAsync(babbleId, promptId, cancellationToken);
    }

    private async Task ValidateBabbleOwnershipAsync(string userId, string babbleId, CancellationToken cancellationToken)
    {
        var babble = await _babbleRepository.GetByIdAsync(userId, babbleId, cancellationToken);
        if (babble is null)
        {
            _logger.LogWarning("Babble {BabbleId} not found for user {UserId}", babbleId, userId);
            throw new InvalidOperationException($"Babble '{babbleId}' not found for user '{userId}'.");
        }
    }
}
