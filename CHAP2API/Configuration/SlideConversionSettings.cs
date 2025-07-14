namespace CHAP2API.Configuration;

public class SlideConversionSettings
{
    public string[] AllowedExtensions { get; set; } = [".ppsx", ".pptx"];
    public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB
    public string DefaultChorusType { get; set; } = "NotSet";
    public string DefaultTimeSignature { get; set; } = "NotSet";
} 