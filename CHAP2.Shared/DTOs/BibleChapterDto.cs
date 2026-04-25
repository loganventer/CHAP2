namespace CHAP2.Shared.DTOs;

public class BibleChapterDto
{
    public BibleBookDto Book { get; set; } = new();
    public int Chapter { get; set; }
    public List<BibleVerseDto> Verses { get; set; } = new();
}
