using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Configuration;
using CHAP2.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Console.Common.Services;

public class ConsoleApplicationService : IConsoleApplicationService
{
    private readonly IApiClientService _apiClientService;
    private readonly ILogger<ConsoleApplicationService> _logger;
    private readonly ConsoleSettings _consoleSettings;
    private ISearchResultsObserver? _resultsObserver;

    public ConsoleApplicationService(
        IApiClientService apiClientService, 
        ILogger<ConsoleApplicationService> logger,
        IOptions<ConsoleSettings> consoleSettings)
    {
        _apiClientService = apiClientService;
        _logger = logger;
        _consoleSettings = consoleSettings.Value;
    }

    public async Task<bool> TestApiConnectivityAsync(CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine("Testing API connectivity...");
        var isConnected = await _apiClientService.TestConnectivityAsync(cancellationToken);
        
        if (isConnected)
        {
            System.Console.WriteLine("API is accessible.");
        }
        else
        {
            System.Console.WriteLine("API is not accessible. Make sure the API is running.");
        }
        
        return isConnected;
    }

    public async Task<Chorus?> ConvertSlideFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine($"Reading file: {filePath}");
        
        var chorus = await _apiClientService.ConvertSlideAsync(filePath, cancellationToken);
        
        if (chorus != null)
        {
            System.Console.WriteLine("Success! Chorus created:");
            DisplayChorus(chorus);
        }
        else
        {
            System.Console.WriteLine("Failed to convert slide file.");
        }
        
        return chorus;
    }

    public async Task<List<Chorus>> SearchChorusesAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var results = await _apiClientService.SearchChorusesAsync(searchTerm, cancellationToken: cancellationToken);
        
        if (results.Any())
        {
            System.Console.WriteLine($"Found {results.Count} choruses:");
            DisplayChoruses(results);
        }
        else
        {
            System.Console.WriteLine("No choruses found.");
        }
        
        return results;
    }

    public async Task RunInteractiveSearchAsync(int searchDelayMs, int minSearchLength, CancellationToken cancellationToken = default)
    {
        var searchString = "";
        var currentResults = new List<Chorus>();
        var searchCancellationTokenSource = new CancellationTokenSource();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);

        // Initial display
        _resultsObserver?.OnResultsChanged(currentResults, searchString);

        Task? lastSearchTask = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsoleKeyInfo key;
            try
            {
                key = System.Console.ReadKey(true);
            }
            catch (InvalidOperationException)
            {
                // Console input is redirected, use fallback mode
                System.Console.WriteLine("\nConsole input is redirected. Using fallback input mode.");
                System.Console.WriteLine("Type your search term and press Enter (or 'quit' to exit):");
                var input = System.Console.ReadLine();
                if (input == null) continue;
                
                if (input.ToLower() == "quit")
                {
                    break;
                }
                
                searchString = input;
                await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                continue;
            }

            // Handle key input
            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    if (currentResults.Count == 1)
                    {
                        var selectedChorus = currentResults[0];
                        System.Console.Clear();
                        System.Console.WriteLine("=== SINGLE CHORUS FOUND ===");
                        DisplayChorus(selectedChorus);
                        System.Console.WriteLine("\nPress any key to continue searching...");
                        System.Console.ReadKey(true);
                        searchString = "";
                        currentResults.Clear();
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    }
                    break;

                case ConsoleKey.Escape:
                    searchString = "";
                    currentResults.Clear();
                    _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    break;

                case ConsoleKey.Backspace:
                    if (searchString.Length > 0)
                    {
                        searchString = searchString[..^1];
                        _logger.LogInformation("Removed character from search string. Current string: '{SearchString}'", searchString);
                        // Update search prompt without clearing screen
                        UpdateSearchPrompt(searchString);
                        await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                    }
                    else
                    {
                        // If search string is empty, clear results and redraw
                        currentResults.Clear();
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    }
                    break;

                default:
                    if (key.KeyChar >= 32 && key.KeyChar <= 126) // Printable characters
                    {
                        searchString += key.KeyChar;
                        _logger.LogInformation("Added character '{Char}' to search string. Current string: '{SearchString}'", key.KeyChar, searchString);
                        // Update search prompt without clearing screen
                        UpdateSearchPrompt(searchString);
                        await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                    }
                    break;
            }
        }
    }

    private void UpdateSearchPrompt(string searchString)
    {
        // Save current cursor position
        var currentLeft = System.Console.CursorLeft;
        var currentTop = System.Console.CursorTop;
        
        // Move to the search prompt line (assuming it's at the top)
        System.Console.SetCursorPosition(0, 2); // After header
        System.Console.Write($"Search: {searchString}");
        // Clear to end of line, ensuring we clear any remaining characters
        var remainingSpace = System.Console.WindowWidth - (8 + searchString.Length);
        if (remainingSpace > 0)
        {
            System.Console.Write(new string(' ', remainingSpace));
        }
        
        // Restore cursor position
        System.Console.SetCursorPosition(currentLeft, currentTop);
    }

    private async Task ProcessSearchString(string searchString, int searchDelayMs, int minSearchLength, 
        List<Chorus> currentResults, CancellationTokenSource searchCancellationTokenSource, 
        CancellationTokenSource cts, Task? lastSearchTask, CancellationToken cancellationToken)
    {
        // Cancel previous search
        searchCancellationTokenSource.Cancel();
        searchCancellationTokenSource = new CancellationTokenSource();
        cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);

        if (lastSearchTask != null && !lastSearchTask.IsCompleted)
        {
            try { await lastSearchTask; } catch { /* ignore */ }
        }

        lastSearchTask = Task.Run(async () =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchString))
                {
                    currentResults.Clear();
                    _logger.LogInformation("Search string is empty, clearing results");
                    _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    return;
                }

                if (searchString.Length < minSearchLength)
                {
                    _logger.LogInformation("Search string '{SearchString}' is too short (length: {Length}, min: {MinLength})", searchString, searchString.Length, minSearchLength);
                    // Don't update observer for short strings, just show the prompt
                    return;
                }

                _logger.LogInformation("Starting search delay for string: '{SearchString}'", searchString);
                await Task.Delay(searchDelayMs, cts.Token);

                _logger.LogInformation("Searching for '{SearchString}' (length: {Length})", searchString, searchString.Length);
                var results = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: cts.Token);
                currentResults = results ?? new List<Chorus>();
                _resultsObserver?.OnResultsChanged(currentResults, searchString);
                _logger.LogInformation("Search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Search for '{SearchString}' was cancelled", searchString);
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search for '{SearchString}'", searchString);
                // Don't update the observer on error, keep previous results
            }
        }, cts.Token);
    }

    public void DisplayChorus(Chorus? chorus)
    {
        if (chorus == null)
        {
            System.Console.WriteLine("(null chorus)");
            return;
        }
        System.Console.WriteLine($"  Id: {chorus.Id}");
        System.Console.WriteLine($"  Name: {chorus.Name ?? "(null)"}");
        System.Console.WriteLine($"  Key: {chorus.Key}");
        System.Console.WriteLine($"  TimeSignature: {chorus.TimeSignature}");
        System.Console.WriteLine($"  Type: {chorus.Type}");
        System.Console.WriteLine($"  ChorusText:");
        System.Console.WriteLine($"  {chorus.ChorusText ?? "(null)"}");
    }

    public void DisplayChoruses(List<Chorus>? choruses)
    {
        if (choruses == null)
        {
            System.Console.WriteLine("(null chorus list)");
            return;
        }
        for (int i = 0; i < choruses.Count; i++)
        {
            System.Console.WriteLine($"--- Chorus {i + 1} ---");
            DisplayChorus(choruses[i]);
            System.Console.WriteLine();
        }
    }

    private string BoldTerm(string input, string term)
    {
        if (string.IsNullOrEmpty(term)) return input;
        var idx = input.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return input;
        var before = input.Substring(0, idx);
        var match = input.Substring(idx, term.Length);
        var after = input.Substring(idx + term.Length);
        return before + "\x1b[1m" + match + "\x1b[0m" + after;
    }

    private void DisplaySearchResult(Chorus chorus, string searchTerm)
    {
        bool found = false;
        // Title
        if (!string.IsNullOrEmpty(chorus.Name) && !string.IsNullOrEmpty(searchTerm) &&
            chorus.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            System.Console.WriteLine($"    Title: {BoldTerm(chorus.Name, searchTerm)}");
            found = true;
        }
        // ChorusText
        if (!string.IsNullOrEmpty(chorus.ChorusText) && !string.IsNullOrEmpty(searchTerm))
        {
            var lines = chorus.ChorusText.Split('\n');
            foreach (var line in lines)
            {
                if (line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    System.Console.WriteLine($"    Text: {BoldTerm(line, searchTerm)}");
                    found = true;
                }
            }
        }
        if (!found)
        {
            System.Console.WriteLine($"    Title: {chorus.Name}");
        }
    }

    public void RegisterResultsObserver(ISearchResultsObserver observer) => _resultsObserver = observer;
} 