using System.Text.Json;
using CHAP2.Application.Helpers;
using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.ValueObjects;
using CHAP2.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class BibleController : ChapControllerAbstractBase
{
    private const int DefaultMaxResults = 100;

    private readonly IBibleQueryService _bibleQueryService;

    public BibleController(
        ILogger<BibleController> logger,
        IBibleQueryService bibleQueryService)
        : base(logger)
    {
        _bibleQueryService = bibleQueryService;
    }

    [HttpGet("books")]
    public async Task<IActionResult> GetBooks(CancellationToken cancellationToken = default)
    {
        LogAction("GetBooks");
        var books = await _bibleQueryService.GetBooksAsync(cancellationToken);
        return Ok(books.Select(ToDto).ToList());
    }

    [HttpGet("books/{bookId}/chapters/{chapter:int}")]
    public async Task<IActionResult> GetChapter(string bookId, int chapter, CancellationToken cancellationToken = default)
    {
        LogAction("GetChapter", new { bookId, chapter });
        var result = await _bibleQueryService.GetChapterAsync(bookId, chapter, cancellationToken);
        if (result is null)
            return NotFound();
        return Ok(ToDto(result));
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] int max = DefaultMaxResults,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search query 'q' is required");

        var sanitized = InputSanitizer.SanitizeSearchQuery(q);
        LogAction("BibleSearch", new { sanitized, max });

        var results = await _bibleQueryService.SearchAsync(sanitized, max, cancellationToken);
        return Ok(new BibleSearchResponseDto
        {
            Query = sanitized,
            Count = results.Count,
            MaxResults = max,
            Results = results.Select(ToDto).ToList(),
        });
    }

    /// <summary>
    /// Server-Sent Events stream of search hits in canonical order, each
    /// tagged with its score. Lets the client render the first row in
    /// tens of milliseconds even on a cold disk cache. Disconnect (or
    /// the user typing another character) cancels the disk walk via the
    /// request cancellation token.
    /// </summary>
    [HttpGet("search/stream")]
    public async Task SearchStream(
        [FromQuery] string? q = null,
        CancellationToken cancellationToken = default)
    {
        Response.StatusCode = string.IsNullOrWhiteSpace(q) ? 400 : 200;
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        // Render's edge proxy buffers responses by default; this header
        // tells nginx-style proxies to stream rather than buffer.
        Response.Headers["X-Accel-Buffering"] = "no";

        if (string.IsNullOrWhiteSpace(q))
        {
            await Response.WriteAsync("event: error\ndata: {\"error\":\"q required\"}\n\n", cancellationToken);
            return;
        }

        var sanitized = InputSanitizer.SanitizeSearchQuery(q);
        LogAction("BibleSearchStream", new { sanitized });

        var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var count = 0;
        try
        {
            await foreach (var hit in _bibleQueryService.StreamSearchAsync(sanitized, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dto = new BibleVerseDto
                {
                    BookId = hit.Verse.BookId,
                    BookName = hit.Verse.BookName,
                    Chapter = hit.Verse.Chapter,
                    Verse = hit.Verse.Verse,
                    Text = hit.Verse.Text,
                    Score = hit.Score,
                };
                var payload = JsonSerializer.Serialize(dto, json);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                count++;
            }
            var donePayload = JsonSerializer.Serialize(new { count }, json);
            await Response.WriteAsync($"event: done\ndata: {donePayload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected -- nothing to do, the response has
            // already been written incrementally.
        }
    }

    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery(Name = "ref")] string? @ref = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(@ref))
            return BadRequest("Reference 'ref' is required");

        var sanitized = InputSanitizer.SanitizeSearchQuery(@ref);
        var resolved = await _bibleQueryService.ResolveReferenceAsync(sanitized, cancellationToken);
        if (resolved is null)
            return NotFound();

        var book = await _bibleQueryService.GetChapterAsync(resolved.BookId, resolved.Chapter, cancellationToken);
        var bookName = book?.Book.Name ?? resolved.BookId;
        return Ok(new BibleReferenceDto
        {
            BookId = resolved.BookId,
            BookName = bookName,
            Chapter = resolved.Chapter,
            Verse = resolved.Verse,
        });
    }

    private static BibleBookDto ToDto(BibleBook book) => new()
    {
        Id = book.Id,
        Name = book.Name,
        EnglishName = book.EnglishName,
        Ordinal = book.Ordinal,
        Testament = book.Testament == BibleTestament.New ? "New" : "Old",
        ChapterCount = book.ChapterCount,
    };

    private static BibleVerseDto ToDto(BibleVerse verse) => new()
    {
        BookId = verse.BookId,
        BookName = verse.BookName,
        Chapter = verse.Chapter,
        Verse = verse.Verse,
        Text = verse.Text,
    };

    private static BibleChapterDto ToDto(BibleChapter chapter) => new()
    {
        Book = ToDto(chapter.Book),
        Chapter = chapter.Number,
        Verses = chapter.Verses.Select(ToDto).ToList(),
    };
}
