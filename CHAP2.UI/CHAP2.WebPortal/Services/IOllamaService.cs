namespace CHAP2.WebPortal.Services;

public interface IOllamaService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt, CancellationToken cancellationToken = default);
} 