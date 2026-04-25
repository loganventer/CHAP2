namespace CHAP2.Infrastructure.DTOs;

internal sealed class BibleChapterDto
{
    public string BookId { get; set; } = string.Empty;
    public string BookName { get; set; } = string.Empty;
    public int Chapter { get; set; }
    public List<BibleVerseDto> Verses { get; set; } = new();
}
