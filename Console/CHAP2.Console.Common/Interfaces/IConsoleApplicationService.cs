using CHAP2.Domain.Entities;

namespace CHAP2.Console.Common.Interfaces;

public interface IConsoleApplicationService
{
    Task<bool> TestApiConnectivityAsync(CancellationToken cancellationToken = default);
    Task<Chorus?> ConvertSlideFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<Chorus>> SearchChorusesAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task RunInteractiveSearchAsync(int searchDelayMs, int minSearchLength, CancellationToken cancellationToken = default);
    void RegisterResultsObserver(ISearchResultsObserver observer);
    void ClearScreenWithDelay(string message = "Goodbye!");
} 