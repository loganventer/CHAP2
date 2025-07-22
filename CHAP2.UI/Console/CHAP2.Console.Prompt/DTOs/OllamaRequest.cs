using System.Text.Json.Serialization;

namespace CHAP2.Console.Prompt.DTOs;

public class OllamaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("options")]
    public OllamaOptions? Options { get; set; }
}

public class OllamaOptions
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("num_predict")]
    public int MaxTokens { get; set; } = 2048;
} 