using System.Text.Json.Serialization;

namespace CHAP2.Console.Vectorize.DTOs;

public class ChorusDataDto
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

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("domainEvents")]
    public List<object> DomainEvents { get; set; } = new();
} 