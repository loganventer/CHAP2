namespace CHAP2.Shared.Configuration;

public class SharedLoggingSettings
{
    public const string DefaultLogLevel = "Information";
    public const string MicrosoftLogLevel = "Warning";
    
    public string Default { get; set; } = DefaultLogLevel;
    public string Microsoft { get; set; } = MicrosoftLogLevel;
} 