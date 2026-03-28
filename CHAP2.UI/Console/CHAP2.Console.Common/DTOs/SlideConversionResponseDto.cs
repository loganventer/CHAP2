using System.Text.Json.Serialization;

namespace CHAP2.Console.Common.DTOs;

public class SlideConversionResponseDto
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("chorus")]
    public ChorusResponseDto? Chorus { get; set; }

    [JsonPropertyName("originalFilename")]
    public string OriginalFilename { get; set; } = string.Empty;
}
