using CHAP2.Domain.Entities;

namespace CHAP2.Application.Models;

/// <summary>
/// A single Bible verse search result paired with its relevance score.
/// Streamed by <see cref="Interfaces.IBibleVerseSearchRepository"/> in
/// canonical order; consumers (batched API, SSE endpoint, client) sort
/// by score on receipt.
/// </summary>
/// <param name="Verse">The matched verse.</param>
/// <param name="Score">3 = exact phrase, 2 = words in order, 1 = words out of order.</param>
public readonly record struct BibleVerseSearchHit(BibleVerse Verse, int Score);
