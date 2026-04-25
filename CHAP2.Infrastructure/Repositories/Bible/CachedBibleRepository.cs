using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.Repositories.Bible;

/// <summary>
/// Caching decorator over <see cref="IBibleRepository"/>. Composition over inheritance:
/// the concrete disk repository is injected and wrapped, mirroring CachedChorusRepository.
/// </summary>
public class CachedBibleRepository : IBibleRepository
{
    private const string AllBooksCacheKey = "bible_books_all";
    private const string ChapterCachePrefix = "bible_chapter_";
    private const string SearchCachePrefix = "bible_search_";

    private static readonly TimeSpan BooksExpiration = TimeSpan.FromHours(12);
    private static readonly TimeSpan ChapterExpiration = TimeSpan.FromHours(12);
    private static readonly TimeSpan SearchExpiration = TimeSpan.FromMinutes(15);

    private readonly IBibleRepository _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedBibleRepository> _logger;

    public CachedBibleRepository(
        IBibleRepository inner,
        IMemoryCache cache,
        ILogger<CachedBibleRepository> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<BibleBook>> GetAllBooksAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllBooksCacheKey, out IReadOnlyList<BibleBook>? cached) && cached is not null)
            return cached;

        var books = await _inner.GetAllBooksAsync(cancellationToken);
        _cache.Set(AllBooksCacheKey, books, BooksExpiration);
        return books;
    }

    public async Task<BibleBook?> GetBookByIdAsync(string bookId, CancellationToken cancellationToken = default)
    {
        var books = await GetAllBooksAsync(cancellationToken);
        return books.FirstOrDefault(b => string.Equals(b.Id, bookId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BibleBook?> GetBookByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;
        var books = await GetAllBooksAsync(cancellationToken);
        return books.FirstOrDefault(b =>
            string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.EnglishName, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(b.Id, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<BibleChapter?> GetChapterAsync(BibleReference reference, CancellationToken cancellationToken = default)
    {
        if (reference is null)
            return null;

        var key = $"{ChapterCachePrefix}{reference.BookId}:{reference.Chapter}";
        if (_cache.TryGetValue(key, out BibleChapter? cached) && cached is not null)
            return cached;

        var chapter = await _inner.GetChapterAsync(reference, cancellationToken);
        if (chapter is not null)
            _cache.Set(key, chapter, ChapterExpiration);
        return chapter;
    }

    public async Task<IReadOnlyList<BibleVerse>> SearchVersesAsync(string query, int max, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || max <= 0)
            return Array.Empty<BibleVerse>();

        var key = $"{SearchCachePrefix}{max}:{query.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(key, out IReadOnlyList<BibleVerse>? cached) && cached is not null)
            return cached;

        var results = await _inner.SearchVersesAsync(query, max, cancellationToken);
        _cache.Set(key, results, SearchExpiration);
        return results;
    }

    /// <summary>
    /// Streaming pass-through. Caching only fits the batched path -- a
    /// stream is single-consumer and we don't want to memoize an entire
    /// (potentially large) result list just to replay it on the next
    /// keystroke. The disk read is fast enough uncached.
    /// </summary>
    public IAsyncEnumerable<BibleVerseSearchHit> StreamSearchAsync(string query, CancellationToken cancellationToken = default)
        => _inner.StreamSearchAsync(query, cancellationToken);
}
