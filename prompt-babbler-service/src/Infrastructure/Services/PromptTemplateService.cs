using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PromptBabbler.Domain.Interfaces;
using PromptBabbler.Domain.Models;

namespace PromptBabbler.Infrastructure.Services;

public sealed class PromptTemplateService : IPromptTemplateService
{
    private const double DefaultCacheDurationMinutes = 5;
    private const string AnonymousUserId = "_anonymous";

    private readonly IPromptTemplateRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PromptTemplateService> _logger;
    private readonly TimeSpan _cacheDuration;

    public PromptTemplateService(
        IPromptTemplateRepository repository,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<PromptTemplateService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;

        var configValue = configuration["PromptTemplates:CacheDurationMinutes"];
        var minutes = double.TryParse(configValue, out var parsed) ? parsed : DefaultCacheDurationMinutes;
        _cacheDuration = TimeSpan.FromMinutes(minutes);
    }

    public async Task<IReadOnlyList<PromptTemplate>> GetTemplatesAsync(
        string? userId,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = userId ?? AnonymousUserId;
        var cacheKey = GetCacheKey(effectiveUserId);

        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
            _logger.LogDebug("Force refresh: cleared cache for user {UserId}", effectiveUserId);
        }

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<PromptTemplate>? cached) && cached is not null)
        {
            _logger.LogDebug("Returning cached templates for user {UserId} ({Count} templates)", effectiveUserId, cached.Count);
            return cached;
        }

        var builtIn = await _repository.GetBuiltInTemplatesAsync(cancellationToken);
        var userTemplates = effectiveUserId != AnonymousUserId
            ? await _repository.GetUserTemplatesAsync(effectiveUserId, cancellationToken)
            : [];

        var merged = builtIn.Concat(userTemplates).ToList().AsReadOnly();

        _cache.Set(cacheKey, (IReadOnlyList<PromptTemplate>)merged, new MemoryCacheEntryOptions
        {
            SlidingExpiration = _cacheDuration,
        });

        _logger.LogDebug("Cached {Count} templates for user {UserId}", merged.Count, effectiveUserId);

        return merged;
    }

    public async Task<(IReadOnlyList<PromptTemplate> Items, string? ContinuationToken)> ListTemplatesAsync(
        string? userId,
        string? continuationToken = null,
        int pageSize = 20,
        string? search = null,
        string? tag = null,
        string? sortBy = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = userId ?? AnonymousUserId;
        return await _repository.ListTemplatesAsync(
            effectiveUserId,
            continuationToken,
            pageSize,
            search,
            tag,
            sortBy,
            sortDirection,
            cancellationToken);
    }

    public async Task<PromptTemplate?> GetByIdAsync(
        string? userId,
        string templateId,
        CancellationToken cancellationToken = default)
    {
        var effectiveUserId = userId ?? AnonymousUserId;

        // Try user partition first (if not anonymous)
        if (effectiveUserId != AnonymousUserId)
        {
            var userTemplate = await _repository.GetByIdAsync(effectiveUserId, templateId, cancellationToken);
            if (userTemplate is not null)
            {
                return userTemplate;
            }
        }

        // Fall back to built-in partition
        return await _repository.GetByIdAsync(CosmosPromptTemplateRepository.BuiltInUserId, templateId, cancellationToken);
    }

    public async Task<PromptTemplate> CreateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateAsync(template, cancellationToken);
        InvalidateCache(template.UserId);
        return result;
    }

    public async Task<PromptTemplate> UpdateAsync(PromptTemplate template, CancellationToken cancellationToken = default)
    {
        var result = await _repository.UpdateAsync(template, cancellationToken);
        InvalidateCache(template.UserId);
        return result;
    }

    public async Task DeleteAsync(string userId, string templateId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(userId, templateId, cancellationToken);
        InvalidateCache(userId);
    }

    private void InvalidateCache(string userId)
    {
        _cache.Remove(GetCacheKey(userId));
        _logger.LogDebug("Invalidated template cache for user {UserId}", userId);
    }

    private static string GetCacheKey(string userId) => $"templates:{userId}";
}
