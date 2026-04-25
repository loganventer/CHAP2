namespace CHAP2.Shared.DTOs;

public class BibleSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MaxResults { get; set; }
    public List<BibleVerseDto> Results { get; set; } = new();
}
