using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class BibleQueryService : IBibleQueryService
{
    private const int DefaultMaxResults = 100;
    private const int HardCapMaxResults = 500;

    private readonly IBibleRepository _bibleRepository;
    private readonly IBibleReferenceParser _referenceParser;
    private readonly ILogger<BibleQueryService> _logger;

    public BibleQueryService(
        IBibleRepository bibleRepository,
        IBibleReferenceParser referenceParser,
        ILogger<BibleQueryService> logger)
    {
        _bibleRepository = bibleRepository ?? throw new ArgumentNullException(nameof(bibleRepository));
        _referenceParser = referenceParser ?? throw new ArgumentNullException(nameof(referenceParser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<BibleBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        return _bibleRepository.GetAllBooksAsync(cancellationToken);
    }

    public async Task<BibleChapter?> GetChapterAsync(string bookId, int chapter, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId) || chapter < 1)
        {
            _logger.LogWarning("Invalid chapter request: bookId={BookId}, chapter={Chapter}", bookId, chapter);
            return null;
        }

        var reference = new BibleReference(bookId, chapter);
        return await _bibleRepository.GetChapterAsync(reference, cancellationToken);
    }

    public Task<IReadOnlyList<BibleVerse>> SearchAsync(string query, int max, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult<IReadOnlyList<BibleVerse>>(Array.Empty<BibleVerse>());

        var clamped = max <= 0 ? DefaultMaxResults : Math.Min(max, HardCapMaxResults);
        return _bibleRepository.SearchVersesAsync(query, clamped, cancellationToken);
    }

    /// <summary>
    /// Stream hits as they're found. No max -- the SSE consumer is
    /// responsible for capping its display. Empty / blank queries yield
    /// nothing.
    /// </summary>
    public IAsyncEnumerable<BibleVerseSearchHit> StreamSearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return EmptyHits();

        return _bibleRepository.StreamSearchAsync(query, cancellationToken);
    }

    private static async IAsyncEnumerable<BibleVerseSearchHit> EmptyHits()
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task<BibleReference?> ResolveReferenceAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult<BibleReference?>(null);

        return _referenceParser.TryParseAsync(input, cancellationToken);
    }
}
