using System.Text.Json.Serialization;

namespace CHAP2.WebPortal.DTOs;

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
    public double Temperature { get; set; } = 0.1;

    [JsonPropertyName("num_predict")]
    public int MaxTokens { get; set; } = 4096;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 0.9;

    [JsonPropertyName("top_k")]
    public int TopK { get; set; } = 40;

    [JsonPropertyName("repeat_penalty")]
    public double RepeatPenalty { get; set; } = 1.1;
} 