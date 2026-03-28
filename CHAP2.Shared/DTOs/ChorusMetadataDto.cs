namespace CHAP2.Shared.DTOs;

public class ChorusMetadataDto
{
    public string? Composer { get; set; }
    public string? Arranger { get; set; }
    public string? Copyright { get; set; }
    public string? Language { get; set; }
    public string? Genre { get; set; }
    public int? Tempo { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
