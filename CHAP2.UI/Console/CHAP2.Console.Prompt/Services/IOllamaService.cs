namespace CHAP2.Console.Prompt.Services;

public interface IOllamaService
{
    Task<string> GenerateResponseAsync(string prompt);
} 