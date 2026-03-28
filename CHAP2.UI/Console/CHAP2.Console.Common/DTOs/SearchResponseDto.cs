using System.Text.Json.Serialization;

namespace CHAP2.Console.Common.DTOs;

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
