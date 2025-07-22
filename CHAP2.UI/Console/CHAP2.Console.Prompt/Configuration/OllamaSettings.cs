namespace CHAP2.Console.Prompt.Configuration;

public class OllamaSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 11434;
    public string Model { get; set; } = "llama3.2";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.7;
} 