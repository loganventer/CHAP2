namespace CHAP2.Shared.DTOs;

public class BibleVerseDto
{
    public string BookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public int Verse { get; set; }
    public string Text { get; set; } = string.Empty;
}
