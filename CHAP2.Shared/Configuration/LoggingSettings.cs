namespace CHAP2.Shared.Configuration;

public class LoggingSettings
{
    public string LogLevel { get; set; } = "Information";
    public bool EnableConsoleLogging { get; set; } = true;
    public bool EnableFileLogging { get; set; } = false;
    public string LogFilePath { get; set; } = "logs/chap2.log";
}
