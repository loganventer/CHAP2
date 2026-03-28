using System.Text.Json.Serialization;

namespace CHAP2.Console.Common.DTOs;

public class ChorusMetadataDto
{
    [JsonPropertyName("composer")]
    public string? Composer { get; set; }

    [JsonPropertyName("arranger")]
    public string? Arranger { get; set; }

    [JsonPropertyName("copyright")]
    public string? Copyright { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("tempo")]
    public int? Tempo { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("customProperties")]
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}
