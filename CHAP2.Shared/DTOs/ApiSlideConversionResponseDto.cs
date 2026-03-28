namespace CHAP2.Shared.DTOs;

public class ApiSlideConversionResponseDto
{
    public string Message { get; set; } = string.Empty;
    public ApiChorusDto Chorus { get; set; } = new();
    public string OriginalFilename { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
