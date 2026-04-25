using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Infrastructure.DTOs;

internal sealed class BibleBookDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EnglishName { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public string Testament { get; set; } = "Old";
    public int ChapterCount { get; set; }
    public string Directory { get; set; } = string.Empty;

    public BibleBook ToEntity() => new(
        Id,
        Name,
        EnglishName,
        Ordinal,
        Enum.TryParse<BibleTestament>(Testament, ignoreCase: true, out var t) ? t : BibleTestament.Old,
        ChapterCount);
}
