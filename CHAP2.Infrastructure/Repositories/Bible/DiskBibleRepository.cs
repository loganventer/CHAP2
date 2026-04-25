using System.Runtime.CompilerServices;
using System.Text.Json;
using CHAP2.Application.Helpers;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using CHAP2.Domain.Entities;
using CHAP2.Domain.ValueObjects;
using CHAP2.Infrastructure.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.Infrastructure.Repositories.Bible;

public class DiskBibleRepository : IBibleRepository
{
    private const string BooksIndexFile = "_books.json";

    private readonly string _rootPath;
    private readonly ILogger<DiskBibleRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public DiskBibleRepository(string rootPath, ILogger<DiskBibleRepository> logger)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("rootPath required", nameof(rootPath));

        _rootPath = Path.IsPathRooted(rootPath)
            ? rootPath
            : Path.Combine(Directory.GetCurrentDirectory(), rootPath);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<BibleBook>> GetAllBooksAsync(CancellationToken cancellationToken = default)
    {
        var dtos = await ReadBooksIndexAsync(cancellationToken);
        return dtos.Select(d => d.ToEntity()).ToList();
    }

    public async Task<BibleBook?> GetBookByIdAsync(string bookId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            return null;

        var dto = await FindBookDtoAsync(bookId, cancellationToken);
        return dto?.ToEntity();
    }

    public async Task<BibleBook?> GetBookByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var dtos = await ReadBooksIndexAsync(cancellationToken);
        var hit = dtos.FirstOrDefault(d =>
            string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.EnglishName, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(d.Id, name, StringComparison.OrdinalIgnoreCase));
        return hit?.ToEntity();
    }

    public async Task<BibleChapter?> GetChapterAsync(BibleReference reference, CancellationToken cancellationToken = default)
    {
        if (reference is null)
            return null;

        var bookDto = await FindBookDtoAsync(reference.BookId, cancellationToken);
        if (bookDto is null)
        {
            _logger.LogDebug("Book {BookId} not found", reference.BookId);
            return null;
        }
        if (reference.Chapter < 1 || reference.Chapter > bookDto.ChapterCount)
        {
            _logger.LogDebug("Chapter {Chapter} out of range for {BookId} (1-{ChapterCount})",
                reference.Chapter, reference.BookId, bookDto.ChapterCount);
            return null;
        }

        var chapterPath = Path.Combine(_rootPath, bookDto.Directory, $"{reference.Chapter:D3}.json");
        if (!File.Exists(chapterPath))
        {
            _logger.LogWarning("Expected chapter file missing: {Path}", chapterPath);
            return null;
        }

        BibleChapterDto? dto;
        try
        {
            await using var stream = File.OpenRead(chapterPath);
            dto = await JsonSerializer.DeserializeAsync<BibleChapterDto>(stream, _jsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read chapter {Path}", chapterPath);
            return null;
        }
        if (dto is null || dto.Verses.Count == 0)
            return null;

        var book = bookDto.ToEntity();
        var verses = dto.Verses
            .Select(v => new BibleVerse(book.Id, book.Name, dto.Chapter, v.Verse, v.Text))
            .ToList();
        return new BibleChapter(book, dto.Chapter, verses);
    }

    private const int ScoreExactPhrase   = 3;
    private const int ScoreInOrderWords  = 2;
    private const int ScoreOutOfOrder    = 1;

    /// <summary>
    /// Top-N by relevance. Drains the streaming variant into memory,
    /// sorts (score desc, normalized verse length asc, then canonical
    /// reading order), then truncates. Existing callers see no behavior
    /// change; the streaming method is the new low-level path.
    /// </summary>
    public async Task<IReadOnlyList<BibleVerse>> SearchVersesAsync(string query, int max, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || max <= 0)
            return Array.Empty<BibleVerse>();

        var collected = new List<ScoredVerse>();
        await foreach (var hit in StreamSearchAsync(query, cancellationToken))
        {
            collected.Add(new ScoredVerse(
                hit.Verse,
                hit.Score,
                BibleTextNormalizer.SearchableText(hit.Verse.Text).Length,
                BookOrdinalFor(hit.Verse.BookId)));
        }

        return collected
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.NormalizedTextLength)
            .ThenBy(s => s.BookOrdinal)
            .ThenBy(s => s.Verse.Chapter)
            .ThenBy(s => s.Verse.Verse)
            .Take(max)
            .Select(s => s.Verse)
            .ToList();
    }

    /// <summary>
    /// Yields hits in canonical (book ordinal -> chapter -> verse) order
    /// as they're found. The disk walk is single-pass so the first hit
    /// surfaces within milliseconds even on a cold cache.
    /// </summary>
    public async IAsyncEnumerable<BibleVerseSearchHit> StreamSearchAsync(
        string query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) yield break;

        var needle = BibleTextNormalizer.SearchableText(query);
        if (needle.Length == 0) yield break;
        var queryWords = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryWords.Length == 0) yield break;

        var dtos = await ReadBooksIndexAsync(cancellationToken);
        foreach (var bookDto in dtos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bookDir = Path.Combine(_rootPath, bookDto.Directory);
            if (!Directory.Exists(bookDir))
                continue;

            for (var ch = 1; ch <= bookDto.ChapterCount; ch++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chapterPath = Path.Combine(bookDir, $"{ch:D3}.json");
                if (!File.Exists(chapterPath))
                    continue;

                BibleChapterDto? chapterDto;
                try
                {
                    await using var stream = File.OpenRead(chapterPath);
                    chapterDto = await JsonSerializer.DeserializeAsync<BibleChapterDto>(stream, _jsonOptions, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping unreadable chapter file {Path}", chapterPath);
                    continue;
                }
                if (chapterDto is null)
                    continue;

                foreach (var verse in chapterDto.Verses)
                {
                    var normalizedText = BibleTextNormalizer.SearchableText(verse.Text);
                    var score = ScoreVerse(normalizedText, needle, queryWords);
                    if (score == 0) continue;
                    yield return new BibleVerseSearchHit(
                        new BibleVerse(bookDto.Id, bookDto.Name, chapterDto.Chapter, verse.Verse, verse.Text),
                        score);
                }
            }
        }
    }

    /// <summary>
    /// 3 = exact normalized phrase, 2 = all words present in order,
    /// 1 = all words present out of order, 0 = at least one word missing.
    /// </summary>
    private static int ScoreVerse(string normalizedText, string needle, string[] queryWords)
    {
        if (normalizedText.Length == 0 || queryWords.Length == 0)
            return 0;
        if (normalizedText.Contains(needle, StringComparison.Ordinal))
            return ScoreExactPhrase;
        if (AllWordsPresentInOrder(normalizedText, queryWords))
            return ScoreInOrderWords;
        for (var i = 0; i < queryWords.Length; i++)
        {
            if (!normalizedText.Contains(queryWords[i], StringComparison.Ordinal))
                return 0;
        }
        return ScoreOutOfOrder;
    }

    private static bool AllWordsPresentInOrder(string text, string[] words)
    {
        var cursor = 0;
        for (var i = 0; i < words.Length; i++)
        {
            var hit = text.IndexOf(words[i], cursor, StringComparison.Ordinal);
            if (hit < 0) return false;
            cursor = hit + words[i].Length;
        }
        return true;
    }

    /// <summary>
    /// Look up a book's canonical ordinal without re-hitting disk. Falls
    /// back to int.MaxValue when missing so unknown books sort last
    /// (shouldn't happen for streamed hits, but defensive).
    /// </summary>
    private int BookOrdinalFor(string bookId)
    {
        var dtos = ReadBooksIndexAsync(CancellationToken.None).GetAwaiter().GetResult();
        var hit = dtos.FirstOrDefault(d => string.Equals(d.Id, bookId, StringComparison.OrdinalIgnoreCase));
        return hit?.Ordinal ?? int.MaxValue;
    }

    private readonly record struct ScoredVerse(BibleVerse Verse, int Score, int NormalizedTextLength, int BookOrdinal);

    private async Task<List<BibleBookDto>> ReadBooksIndexAsync(CancellationToken cancellationToken)
    {
        var indexPath = Path.Combine(_rootPath, BooksIndexFile);
        if (!File.Exists(indexPath))
        {
            _logger.LogError("Bible books index missing at {Path}", indexPath);
            return new List<BibleBookDto>();
        }

        await using var stream = File.OpenRead(indexPath);
        var dtos = await JsonSerializer.DeserializeAsync<List<BibleBookDto>>(stream, _jsonOptions, cancellationToken);
        return dtos ?? new List<BibleBookDto>();
    }

    private async Task<BibleBookDto?> FindBookDtoAsync(string bookId, CancellationToken cancellationToken)
    {
        var dtos = await ReadBooksIndexAsync(cancellationToken);
        return dtos.FirstOrDefault(d => string.Equals(d.Id, bookId, StringComparison.OrdinalIgnoreCase));
    }
}
