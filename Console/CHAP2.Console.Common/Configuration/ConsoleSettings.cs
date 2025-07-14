namespace CHAP2.Console.Common.Configuration;

public class ConsoleSettings
{
    public int MaxDisplayResults { get; set; } = 10;
    public bool ClearScreenOnSearch { get; set; } = true;
    public bool ShowSearchDelay { get; set; } = true;
    public bool ShowMinSearchLength { get; set; } = true;
    public bool ForceFallbackInputMode { get; set; } = false;
} 