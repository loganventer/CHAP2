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
        System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
        System.Console.WriteLine("=============================================");
        
        if (_consoleSettings.ShowSearchDelay)
        {
            System.Console.WriteLine($"Search Delay: {searchDelayMs}ms");
        }
        
        if (_consoleSettings.ShowMinSearchLength)
        {
            System.Console.WriteLine($"Minimum Search Length: {minSearchLength}");
        }
        
        System.Console.WriteLine();
        System.Console.WriteLine("Type to search choruses. Search triggers after each keystroke with delay.");
        System.Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.\n");

        var searchString = "";
        var currentResults = new List<Chorus>();
        var searchCancellationTokenSource = new CancellationTokenSource();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);

        System.Console.Write($"Search: {searchString}");

        Task? lastSearchTask = null;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Check if we should force fallback mode
            if (_consoleSettings.ForceFallbackInputMode)
            {
                System.Console.WriteLine("Fallback input mode enabled.");
                System.Console.WriteLine("Type your search term and press Enter (or 'quit' to exit):");
                var input = System.Console.ReadLine();
                if (input == null) continue;
                
                if (input.ToLower() == "quit")
                {
                    break;
                }
                
                searchString = input;
                _logger.LogInformation("Forced fallback mode: Processing search string '{SearchString}'", searchString);
                System.Console.WriteLine($"Searching for: {searchString}");
                
                // Cancel previous search
                searchCancellationTokenSource.Cancel();
                searchCancellationTokenSource = new CancellationTokenSource();
                cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);
                var fallbackToken1 = cts.Token;

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
                            _logger.LogInformation("Forced fallback mode: Search string is empty, clearing results");
                            System.Console.WriteLine("Search cleared.");
                            System.Console.Write($"Search: {searchString}");
                            return;
                        }

                        if (searchString.Length < minSearchLength)
                        {
                            _logger.LogInformation("Forced fallback mode: Search string '{SearchString}' is too short (length: {Length}, min: {MinLength})", searchString, searchString.Length, minSearchLength);
                            System.Console.WriteLine($"Type at least {minSearchLength} characters to search");
                            System.Console.Write($"Search: {searchString}");
                            return;
                        }

                        _logger.LogInformation("Forced fallback mode: Searching for '{SearchString}' (length: {Length})", searchString, searchString.Length);
                        var results = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: fallbackToken1);
                        currentResults = results ?? new List<Chorus>();
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        _logger.LogInformation("Forced fallback mode: Search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);

                        if (!string.IsNullOrWhiteSpace(searchString))
                        {
                            // Only clear screen and show results if we have results
                            if (currentResults.Any())
                            {
                                System.Console.Clear();
                                var displayCount = Math.Min(currentResults.Count, _consoleSettings.MaxDisplayResults);
                                for (int i = 0; i < displayCount; i++)
                                {
                                    var chorus = currentResults[i];
                                    System.Console.WriteLine($"{i + 1}.");
                                    DisplaySearchResult(chorus, searchString);
                                }
                                if (currentResults.Count > _consoleSettings.MaxDisplayResults)
                                {
                                    System.Console.WriteLine($"  ... and {currentResults.Count - _consoleSettings.MaxDisplayResults} more");
                                }
                            }
                            // If no results, just continue typing without clearing screen
                        }
                        System.Console.Write($"Search: {searchString}");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Forced fallback mode: Search for '{SearchString}' was cancelled", searchString);
                        // Search was cancelled, ignore
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Forced fallback mode: Error during search for '{SearchString}'", searchString);
                        System.Console.WriteLine(" (Error occurred)");
                        System.Console.Write($"Search: {searchString}");
                    }
                }, fallbackToken1);
                await lastSearchTask;
                continue;
            }

            ConsoleKeyInfo key;
            try
            {
                key = System.Console.ReadKey(true);
            }
            catch (InvalidOperationException)
            {
                // Console input is redirected, use Console.Read() as fallback
                System.Console.WriteLine("\nConsole input is redirected. Using fallback input mode.");
                System.Console.WriteLine("Type your search term and press Enter (or 'quit' to exit):");
                var input = System.Console.ReadLine();
                if (input == null) continue;
                
                if (input.ToLower() == "quit")
                {
                    break;
                }
                
                // Simulate character-by-character input by processing each character
                foreach (char c in input)
                {
                    if (c >= 32 && c <= 126) // Printable characters
                    {
                        searchString += c;
                        System.Console.Write(c);
                        _logger.LogInformation("Added character '{Char}' to search string. Current string: '{SearchString}'", c, searchString);
                        
                        // Cancel previous search
                        searchCancellationTokenSource.Cancel();
                        searchCancellationTokenSource = new CancellationTokenSource();
                        cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);
                        var fallbackToken2 = cts.Token;

                        if (lastSearchTask != null && !lastSearchTask.IsCompleted)
                        {
                            try { await lastSearchTask; } catch { /* ignore */ }
                        }

                        lastSearchTask = Task.Run(async () =>
                        {
                            try
                            {
                                _logger.LogInformation("Starting search delay for string: '{SearchString}'", searchString);
                                await Task.Delay(searchDelayMs, fallbackToken2);
                                
                                if (string.IsNullOrWhiteSpace(searchString))
                                {
                                    currentResults.Clear();
                                    _logger.LogInformation("Search string is empty, clearing results");
                                    return;
                                }

                                if (searchString.Length < minSearchLength)
                                {
                                    _logger.LogInformation("Search string '{SearchString}' is too short (length: {Length}, min: {MinLength})", searchString, searchString.Length, minSearchLength);
                                    return;
                                }

                                _logger.LogInformation("Searching for: '{SearchString}' (length: {Length})", searchString, searchString.Length);
                                var results = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: fallbackToken2);
                                currentResults = results ?? new List<Chorus>();
                                _resultsObserver?.OnResultsChanged(currentResults, searchString);
                                _logger.LogInformation("Search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);

                                                        if (!string.IsNullOrWhiteSpace(searchString))
                        {
                            // Only clear screen and show results if we have results
                            if (currentResults.Any())
                            {
                                System.Console.Clear();
                                var displayCount = Math.Min(currentResults.Count, _consoleSettings.MaxDisplayResults);
                                for (int i = 0; i < displayCount; i++)
                                {
                                    var chorus = currentResults[i];
                                    System.Console.WriteLine($"{i + 1}.");
                                    DisplaySearchResult(chorus, searchString);
                                }
                                if (currentResults.Count > _consoleSettings.MaxDisplayResults)
                                {
                                    System.Console.WriteLine($"  ... and {currentResults.Count - _consoleSettings.MaxDisplayResults} more");
                                }
                            }
                            // If no results, just continue typing without clearing screen
                        }
                        System.Console.Write($"Search: {searchString}");
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.LogInformation("Search for '{SearchString}' was cancelled", searchString);
                                // Search was cancelled, ignore
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error during search for '{SearchString}'", searchString);
                                System.Console.WriteLine(" (Error occurred)");
                            }
                        }, fallbackToken2);
                        await lastSearchTask;
                    }
                }
                
                // Clear for next input
                searchString = "";
                currentResults.Clear();
                System.Console.WriteLine();
                System.Console.Write($"Search: {searchString}");
                continue;
            }
            
            if (key.Key == ConsoleKey.Enter)
            {
                if (currentResults.Count == 1)
                {
                    System.Console.WriteLine("\n=== SINGLE CHORUS FOUND ===");
                    DisplayChorus(currentResults[0]);
                    System.Console.WriteLine("\nPress any key to continue searching...");
                    System.Console.ReadKey(true);
                    
                    // Clear and start new search
                    searchString = "";
                    currentResults.Clear();
                    searchCancellationTokenSource.Cancel();
                    searchCancellationTokenSource = new CancellationTokenSource();
                    cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);
                    
                    if (_consoleSettings.ClearScreenOnSearch)
                    {
                        System.Console.Clear();
                    }
                    System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
                    System.Console.WriteLine("=============================================");
                    
                    if (_consoleSettings.ShowSearchDelay)
                    {
                        System.Console.WriteLine($"Search Delay: {searchDelayMs}ms");
                    }
                    
                    if (_consoleSettings.ShowMinSearchLength)
                    {
                        System.Console.WriteLine($"Minimum Search Length: {minSearchLength}");
                    }
                    
                    System.Console.WriteLine();
                    System.Console.WriteLine("Type to search choruses. Search triggers after each keystroke with delay.");
                    System.Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.\n");
                    System.Console.Write($"Search: {searchString}");
                }
                else if (currentResults.Count > 1)
                {
                    System.Console.WriteLine($"\nMultiple choruses found ({currentResults.Count}). Continue typing to narrow down.");
                    System.Console.Write($"Search: {searchString}");
                }
                else
                {
                    System.Console.WriteLine("\nNo results found. Continue typing to search.");
                    System.Console.Write($"Search: {searchString}");
                }
                continue;
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                searchString = "";
                currentResults.Clear();
                searchCancellationTokenSource.Cancel();
                searchCancellationTokenSource = new CancellationTokenSource();
                cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);
                System.Console.WriteLine("\nSearch cleared.");
                System.Console.Write($"Search: {searchString}");
                continue;
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (searchString.Length > 0)
                {
                    searchString = searchString[..^1];
                    System.Console.Write("\b \b");
                }
            }
            else if (key.KeyChar >= 32 && key.KeyChar <= 126) // Printable characters
            {
                searchString += key.KeyChar;
                System.Console.Write(key.KeyChar);
                _logger.LogInformation("Added character '{Char}' to search string. Current string: '{SearchString}'", key.KeyChar, searchString);
            }

            // Cancel previous search and clear screen
            searchCancellationTokenSource.Cancel();
            searchCancellationTokenSource = new CancellationTokenSource();
            cts = CancellationTokenSource.CreateLinkedTokenSource(searchCancellationTokenSource.Token);
            var token = cts.Token;

            if (lastSearchTask != null && !lastSearchTask.IsCompleted)
            {
                try { await lastSearchTask; } catch { /* ignore */ }
            }

            lastSearchTask = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting search delay for string: '{SearchString}'", searchString);
                    await Task.Delay(searchDelayMs, token);
                    
                    if (string.IsNullOrWhiteSpace(searchString))
                    {
                        currentResults.Clear();
                        _logger.LogInformation("Search string is empty, clearing results");
                        if (_consoleSettings.ClearScreenOnSearch)
                        {
                            System.Console.Clear();
                        }
                        System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
                        System.Console.WriteLine("=============================================");
                        
                        if (_consoleSettings.ShowSearchDelay)
                        {
                            System.Console.WriteLine($"Search Delay: {searchDelayMs}ms");
                        }
                        
                        if (_consoleSettings.ShowMinSearchLength)
                        {
                            System.Console.WriteLine($"Minimum Search Length: {minSearchLength}");
                        }
                        
                        System.Console.WriteLine();
                        System.Console.WriteLine("Type to search choruses. Search triggers after each keystroke with delay.");
                        System.Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.\n");
                        System.Console.Write($"Search: {searchString}");
                        return;
                    }

                    // Clear screen and show current search status
                    if (_consoleSettings.ClearScreenOnSearch)
                    {
                        System.Console.Clear();
                    }
                    System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
                    System.Console.WriteLine("=============================================");
                    
                    if (_consoleSettings.ShowSearchDelay)
                    {
                        System.Console.WriteLine($"Search Delay: {searchDelayMs}ms");
                    }
                    
                    if (_consoleSettings.ShowMinSearchLength)
                    {
                        System.Console.WriteLine($"Minimum Search Length: {minSearchLength}");
                    }
                    
                    System.Console.WriteLine();
                    System.Console.WriteLine("Type to search choruses. Search triggers after each keystroke with delay.");
                    System.Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.\n");
                    System.Console.Write($"Search: {searchString}");

                    if (searchString.Length < minSearchLength)
                    {
                        _logger.LogInformation("Search string '{SearchString}' is too short (length: {Length}, min: {MinLength})", searchString, searchString.Length, minSearchLength);
                        System.Console.WriteLine($" (Type at least {minSearchLength} characters to search)");
                        return;
                    }

                    // Make the API call only if minimum length is met
                    _logger.LogInformation("Searching for: '{SearchString}' (length: {Length})", searchString, searchString.Length);
                    var results = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: token);
                    currentResults = results ?? new List<Chorus>();
                    _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    _logger.LogInformation("Search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);

                    if (!string.IsNullOrWhiteSpace(searchString))
                    {
                        // Only clear screen and show results if we have results
                        if (currentResults.Any())
                        {
                            System.Console.Clear();
                            var displayCount = Math.Min(currentResults.Count, _consoleSettings.MaxDisplayResults);
                            for (int i = 0; i < displayCount; i++)
                            {
                                var chorus = currentResults[i];
                                System.Console.WriteLine($"{i + 1}.");
                                DisplaySearchResult(chorus, searchString);
                            }
                            if (currentResults.Count > _consoleSettings.MaxDisplayResults)
                            {
                                System.Console.WriteLine($"  ... and {currentResults.Count - _consoleSettings.MaxDisplayResults} more");
                            }
                        }
                        // If no results, just continue typing without clearing screen
                    }
                    System.Console.Write($"Search: {searchString}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Search for '{SearchString}' was cancelled", searchString);
                    // Search was cancelled, ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during search for '{SearchString}'", searchString);
                    System.Console.WriteLine(" (Error occurred)");
                }
            }, token);
            await lastSearchTask;
        }
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