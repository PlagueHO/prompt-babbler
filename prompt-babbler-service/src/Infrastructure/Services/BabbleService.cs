using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class BabbleService : IBabbleService
{
    private readonly IBabbleRepository _babbleRepository;
    private readonly IGeneratedPromptRepository _generatedPromptRepository;
    private readonly ILogger<BabbleService> _logger;

    public BabbleService(
        IBabbleRepository babbleRepository,
        IGeneratedPromptRepository generatedPromptRepository,
        ILogger<BabbleService> logger)
    {
        _babbleRepository = babbleRepository;
        _generatedPromptRepository = generatedPromptRepository;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<Babble> Items, string? ContinuationToken)> GetByUserAsync(
        string userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? sortBy = null,
        string? sortDirection = null,
        bool? isPinned = null,
        CancellationToken cancellationToken = default)
    {
        return await _babbleRepository.GetByUserAsync(userId, continuationToken, pageSize, search, sortBy, sortDirection, isPinned, cancellationToken);
    }

    public async Task<Babble?> GetByIdAsync(string userId, string babbleId, CancellationToken cancellationToken = default)
    {
        return await _babbleRepository.GetByIdAsync(userId, babbleId, cancellationToken);
    }

    public async Task<Babble> CreateAsync(Babble babble, CancellationToken cancellationToken = default)
    {
        return await _babbleRepository.CreateAsync(babble, cancellationToken);
    }

    public async Task<Babble> UpdateAsync(string userId, Babble babble, CancellationToken cancellationToken = default)
    {
        var existing = await _babbleRepository.GetByIdAsync(userId, babble.Id, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Babble '{babble.Id}' not found for user '{userId}'.");
        }

        return await _babbleRepository.UpdateAsync(babble, cancellationToken);
    }

    public async Task<Babble> SetPinAsync(string userId, string babbleId, bool isPinned, CancellationToken cancellationToken = default)
    {
        return await _babbleRepository.SetPinAsync(userId, babbleId, isPinned, cancellationToken);
    }

    public async Task DeleteAsync(string userId, string babbleId, CancellationToken cancellationToken = default)
    {
        // Cascade delete: remove all generated prompts for this babble first
        await _generatedPromptRepository.DeleteByBabbleAsync(babbleId, cancellationToken);
        _logger.LogInformation("Cascade deleted generated prompts for babble {BabbleId}", babbleId);

        await _babbleRepository.DeleteAsync(userId, babbleId, cancellationToken);
    }
}
