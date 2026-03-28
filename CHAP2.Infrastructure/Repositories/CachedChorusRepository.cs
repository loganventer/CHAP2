using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.Repositories;

/// <summary>
/// Caching decorator for IChorusRepository that provides memory caching
/// for frequently accessed choruses.
/// </summary>
public class CachedChorusRepository : IChorusRepository
{
    private readonly IChorusRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedChorusRepository> _logger;

    private const string AllChorusesCacheKey = "all_choruses";
    private const string ChorusByIdPrefix = "chorus_id_";
    private const string ChorusByNamePrefix = "chorus_name_";
    private const string ChorusByKeyPrefix = "chorus_key_";
    private const string ChorusByTimeSignaturePrefix = "chorus_timesig_";
    private const string ChorusCountCacheKey = "chorus_count";

    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan AllChorusesExpiration = TimeSpan.FromMinutes(2);

    public CachedChorusRepository(
        IChorusRepository innerRepository,
        IMemoryCache cache,
        ILogger<CachedChorusRepository> logger)
    {
        _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Chorus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChorusByIdPrefix}{id}";

        if (_cache.TryGetValue(cacheKey, out Chorus? cachedChorus))
        {
            _logger.LogDebug("Cache hit for chorus ID {ChorusId}", id);
            return cachedChorus;
        }

        _logger.LogDebug("Cache miss for chorus ID {ChorusId}", id);
        var chorus = await _innerRepository.GetByIdAsync(id, cancellationToken);

        if (chorus != null)
        {
            _cache.Set(cacheKey, chorus, DefaultCacheExpiration);
        }

        return chorus;
    }

    public async Task<Chorus?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChorusByNamePrefix}{name.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out Chorus? cachedChorus))
        {
            _logger.LogDebug("Cache hit for chorus name {ChorusName}", name);
            return cachedChorus;
        }

        _logger.LogDebug("Cache miss for chorus name {ChorusName}", name);
        var chorus = await _innerRepository.GetByNameAsync(name, cancellationToken);

        if (chorus != null)
        {
            _cache.Set(cacheKey, chorus, DefaultCacheExpiration);
        }

        return chorus;
    }

    public async Task<IReadOnlyList<Chorus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllChorusesCacheKey, out IReadOnlyList<Chorus>? cachedChoruses))
        {
            _logger.LogDebug("Cache hit for all choruses");
            return cachedChoruses!;
        }

        _logger.LogDebug("Cache miss for all choruses");
        var choruses = await _innerRepository.GetAllAsync(cancellationToken);
        _cache.Set(AllChorusesCacheKey, choruses, AllChorusesExpiration);

        return choruses;
    }

    public async Task<IReadOnlyList<Chorus>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        // For paginated queries, we can use the cached all-choruses list and paginate in memory
        var all = await GetAllAsync(cancellationToken);
        return all.Skip(skip).Take(take).ToList();
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(ChorusCountCacheKey, out int cachedCount))
        {
            _logger.LogDebug("Cache hit for chorus count");
            return cachedCount;
        }

        _logger.LogDebug("Cache miss for chorus count");
        var count = await _innerRepository.GetCountAsync(cancellationToken);
        _cache.Set(ChorusCountCacheKey, count, AllChorusesExpiration);

        return count;
    }

    public async Task<IReadOnlyList<Chorus>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.GetByIdsAsync(ids, cancellationToken);
    }

    public async Task<Chorus> AddAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(chorus, cancellationToken);
        InvalidateCache();
        return result;
    }

    public async Task<Chorus> UpdateAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.UpdateAsync(chorus, cancellationToken);
        InvalidateCacheForChorus(chorus.Id, chorus.Name);
        return result;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var chorus = await GetByIdAsync(id, cancellationToken);
        await _innerRepository.DeleteAsync(id, cancellationToken);

        if (chorus != null)
        {
            InvalidateCacheForChorus(id, chorus.Name);
        }
        else
        {
            InvalidateCache();
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.ExistsAsync(name, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.ExistsAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<Chorus>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.SearchAsync(searchTerm, cancellationToken);
    }

    public async Task<IReadOnlyList<Chorus>> GetByKeyAsync(MusicalKey key, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChorusByKeyPrefix}{key}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Chorus>? cachedChoruses))
        {
            _logger.LogDebug("Cache hit for choruses with key {Key}", key);
            return cachedChoruses!;
        }

        _logger.LogDebug("Cache miss for choruses with key {Key}", key);
        var choruses = await _innerRepository.GetByKeyAsync(key, cancellationToken);
        _cache.Set(cacheKey, choruses, DefaultCacheExpiration);

        return choruses;
    }

    public async Task<IReadOnlyList<Chorus>> GetByTimeSignatureAsync(TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChorusByTimeSignaturePrefix}{timeSignature}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<Chorus>? cachedChoruses))
        {
            _logger.LogDebug("Cache hit for choruses with time signature {TimeSignature}", timeSignature);
            return cachedChoruses!;
        }

        _logger.LogDebug("Cache miss for choruses with time signature {TimeSignature}", timeSignature);
        var choruses = await _innerRepository.GetByTimeSignatureAsync(timeSignature, cancellationToken);
        _cache.Set(cacheKey, choruses, DefaultCacheExpiration);

        return choruses;
    }

    private void InvalidateCache()
    {
        _logger.LogDebug("Invalidating all chorus cache entries");
        _cache.Remove(AllChorusesCacheKey);
        _cache.Remove(ChorusCountCacheKey);
        // Note: Key and TimeSignature caches will naturally expire or can be invalidated if needed
    }

    private void InvalidateCacheForChorus(Guid id, string name)
    {
        _logger.LogDebug("Invalidating cache for chorus {ChorusId} ({ChorusName})", id, name);
        _cache.Remove(AllChorusesCacheKey);
        _cache.Remove($"{ChorusByIdPrefix}{id}");
        _cache.Remove($"{ChorusByNamePrefix}{name.ToLowerInvariant()}");
    }
}
