namespace CHAP2.Shared.Configuration;

public class SlideConversionSettings
{
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedExtensions { get; set; } = { ".pptx", ".ppsx" };
    public int MaxConcurrentConversions { get; set; } = 3;
}
