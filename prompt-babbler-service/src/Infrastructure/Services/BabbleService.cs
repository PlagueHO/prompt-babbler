using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class BabbleService : IBabbleService
{
    /// <summary>
    /// Minimum word count in the search query before vector (semantic) search is used.
    /// Queries below both thresholds use keyword-only search, avoiding an embedding API call.
    /// May be promoted to configuration if tuning is needed.
    /// </summary>
    private const int VectorSearchMinWords = 2;

    /// <summary>
    /// Minimum character length in the search query before vector (semantic) search is used.
    /// Queries below both thresholds use keyword-only search, avoiding an embedding API call.
    /// May be promoted to configuration if tuning is needed.
    /// </summary>
    private const int VectorSearchMinLength = 10;

    private readonly IBabbleRepository _babbleRepository;
    private readonly IGeneratedPromptRepository _generatedPromptRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<BabbleService> _logger;

    public BabbleService(
        IBabbleRepository babbleRepository,
        IGeneratedPromptRepository generatedPromptRepository,
        IEmbeddingService embeddingService,
        ILogger<BabbleService> logger)
    {
        _babbleRepository = babbleRepository;
        _generatedPromptRepository = generatedPromptRepository;
        _embeddingService = embeddingService;
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
        var babbleToSave = await TryAddEmbeddingAsync(babble, "Saving without vector.", cancellationToken);

        return await _babbleRepository.CreateAsync(babbleToSave, cancellationToken);
    }

    public async Task<Babble> UpsertAsync(Babble babble, CancellationToken cancellationToken = default)
    {
        var babbleToSave = await TryAddEmbeddingAsync(babble, "Upserting without vector.", cancellationToken);
        return await _babbleRepository.UpsertAsync(babbleToSave, cancellationToken);
    }

    public async Task<Babble> UpdateAsync(string userId, Babble babble, CancellationToken cancellationToken = default)
    {
        var existing = await _babbleRepository.GetByIdAsync(userId, babble.Id, cancellationToken);
        if (existing is null)
        {
            throw new InvalidOperationException($"Babble '{babble.Id}' not found for user '{userId}'.");
        }

        var babbleToSave = babble;
        try
        {
            var vector = await _embeddingService.GenerateEmbeddingAsync(babble.Text, cancellationToken);
            babbleToSave = babble with { ContentVector = vector.ToArray() };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for babble {BabbleId}. Saving without updated vector.", babble.Id);
        }

        return await _babbleRepository.UpdateAsync(babbleToSave, cancellationToken);
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

    public async Task<IReadOnlyList<BabbleSearchResult>> SearchAsync(string userId, string query, int topN, CancellationToken cancellationToken = default)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var useVector = words.Length >= VectorSearchMinWords || query.Length >= VectorSearchMinLength;

        if (!useVector)
        {
            return await _babbleRepository.SearchByKeywordAsync(userId, query, topN, cancellationToken);
        }

        // Keyword search runs immediately in parallel with embedding generation.
        var keywordTask = _babbleRepository.SearchByKeywordAsync(userId, query, topN, cancellationToken);

        var vector = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
        var vectorTask = _babbleRepository.SearchByVectorAsync(userId, vector, topN, cancellationToken);

        await Task.WhenAll(keywordTask, vectorTask);

        // Merge both result sets, deduplicating by babble ID and keeping the higher score.
        var merged = new Dictionary<string, BabbleSearchResult>();
        foreach (var result in keywordTask.Result.Concat(vectorTask.Result))
        {
            if (!merged.TryGetValue(result.Babble.Id, out var existing) || result.SimilarityScore > existing.SimilarityScore)
            {
                merged[result.Babble.Id] = result;
            }
        }

        return merged.Values.OrderByDescending(r => r.SimilarityScore).ToList().AsReadOnly();
    }

    private async Task<Babble> TryAddEmbeddingAsync(Babble babble, string warningSuffix, CancellationToken cancellationToken)
    {
        try
        {
            var vector = await _embeddingService.GenerateEmbeddingAsync(babble.Text, cancellationToken);
            return babble with { ContentVector = vector.ToArray() };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for babble {BabbleId}. {WarningSuffix}", babble.Id, warningSuffix);
            return babble;
        }
    }
}
