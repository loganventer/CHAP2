namespace CHAP2.WebPortal.Configuration;

public class OllamaSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 11434;
    public string Model { get; set; } = "phi3";
    public int MaxTokens { get; set; } = 4096;
    public double Temperature { get; set; } = 0.1;
    public double TopP { get; set; } = 0.9;
    public int TopK { get; set; } = 40;
    public double RepeatPenalty { get; set; } = 1.1;
    public int TimeoutSeconds { get; set; } = 300;
} 