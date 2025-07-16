using System.Text.Json.Serialization;

namespace CHAP2.Console.Common.DTOs;

public class ChorusResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("chorusText")]
    public string ChorusText { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public int Key { get; set; }

    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("timeSignature")]
    public int TimeSignature { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }

    [JsonPropertyName("metadata")]
    public ChorusMetadataDto? Metadata { get; set; }

    [JsonPropertyName("domainEvents")]
    public List<object> DomainEvents { get; set; } = new();
}

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

public class SearchResponseDto
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("searchMode")]
    public string SearchMode { get; set; } = string.Empty;

    [JsonPropertyName("searchIn")]
    public string SearchIn { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("results")]
    public List<ChorusResponseDto> Results { get; set; } = new();
}

public class SlideConversionResponseDto
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("chorus")]
    public ChorusResponseDto? Chorus { get; set; }

    [JsonPropertyName("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;
} 