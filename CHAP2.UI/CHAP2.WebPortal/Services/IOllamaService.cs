namespace CHAP2.WebPortal.Services;

public interface IOllamaService
{
    Task<string> GenerateResponseAsync(string prompt);
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt);
} 