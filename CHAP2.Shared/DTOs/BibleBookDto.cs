namespace CHAP2.Shared.DTOs;

public class BibleBookDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public string Testament { get; set; } = "Old";
    public int ChapterCount { get; set; }
}
