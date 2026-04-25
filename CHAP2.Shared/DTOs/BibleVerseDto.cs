namespace CHAP2.Shared.DTOs;

public class BibleVerseDto
{
    public string BookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Search relevance: 3 = exact phrase, 2 = words in order, 1 = words
    /// out of order, 0 = not from a search (e.g. chapter render).
    /// Streaming search emits hits in canonical order with this score so
    /// the client can re-rank locally as matches arrive.
    /// </summary>
    public int Score { get; set; }
}
