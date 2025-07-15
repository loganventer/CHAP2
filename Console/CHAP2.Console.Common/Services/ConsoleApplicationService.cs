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
    private readonly ISelectionService _selectionService;
    private readonly IConsoleDisplayService _displayService;
    private ISearchResultsObserver? _resultsObserver;

    public ConsoleApplicationService(
        IApiClientService apiClientService, 
        ILogger<ConsoleApplicationService> logger,
        IOptions<ConsoleSettings> consoleSettings,
        ISelectionService selectionService,
        IConsoleDisplayService displayService)
    {
        _apiClientService = apiClientService;
        _logger = logger;
        _consoleSettings = consoleSettings.Value;
        _selectionService = selectionService;
        _displayService = displayService;
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
            _displayService.DisplayChorus(chorus);
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
            _displayService.DisplayChoruses(results);
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
        var isInDetailView = false;
        var selectedChorus = (Chorus?)null;
        var numberBuffer = "";
        var lastNumberKeyTime = DateTime.MinValue;

        // Enable cursor visibility
        System.Console.CursorVisible = false;

        // Initial display with proper cursor positioning
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
                case ConsoleKey.C when (key.Modifiers & ConsoleModifiers.Control) != 0:
                    // Ctrl+C - exit the application
                    _logger.LogInformation("Ctrl+C pressed - exiting application");
                    return;
                    
                case ConsoleKey.UpArrow:
                    if (!isInDetailView)
                    {
                        _logger.LogInformation("Up arrow pressed - moving selection up");
                        _selectionService.MoveUp();
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (!isInDetailView)
                    {
                        _logger.LogInformation("Down arrow pressed - moving selection down");
                        _selectionService.MoveDown();
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    }
                    break;

                case ConsoleKey.Enter:
                    if (!isInDetailView && currentResults.Count > 0)
                    {
                        var selectedIndex = _selectionService.SelectedIndex;
                        _logger.LogInformation("Enter pressed - selected index: {SelectedIndex}, total results: {TotalResults}", selectedIndex, currentResults.Count);
                        if (selectedIndex >= 0 && selectedIndex < currentResults.Count)
                        {
                            selectedChorus = currentResults[selectedIndex];
                            isInDetailView = true;
                            _selectionService.IsInDetailView = true;
                            _displayService.DisplayChorusDetail(selectedChorus);
                            _logger.LogInformation("Entered detail view for chorus: {ChorusName}", selectedChorus.Name);
                        }
                    }
                    else if (isInDetailView)
                    {
                        // If already in detail view, Enter does nothing (user needs to press Escape)
                        _logger.LogInformation("Enter pressed in detail view - ignored");
                        break;
                    }
                    break;

                case ConsoleKey.Escape:
                    if (isInDetailView)
                    {
                        // Return to search view and clear search
                        ReturnToSearch(ref isInDetailView, ref selectedChorus, ref searchString, ref currentResults);
                    }
                    else
                    {
                        // Quit the application when Escape is pressed on main screen
                        _logger.LogInformation("Escape pressed on main screen - exiting application");
                        return;
                    }
                    break;

                case ConsoleKey.Backspace:
                    if (!isInDetailView)
                    {
                        if (searchString.Length > 0)
                        {
                            searchString = searchString[..^1];
                            _logger.LogInformation("Removed character from search string. Current string: '{SearchString}'", searchString);
                            // Let the observer handle the UI update
                            await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                        }
                        else
                        {
                            // If search string is empty, clear results and redraw
                            currentResults.Clear();
                            _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        }
                    }
                    break;

                default:
                    if (!isInDetailView)
                    {
                        // Handle number keys for song selection
                        if (key.KeyChar >= '0' && key.KeyChar <= '9')
                        {
                            var now = DateTime.Now;
                            
                            // If it's been more than 3 seconds since the last number key, clear the buffer
                            if ((now - lastNumberKeyTime).TotalSeconds > 3)
                            {
                                numberBuffer = "";
                            }
                            
                            numberBuffer += key.KeyChar;
                            lastNumberKeyTime = now;
                            
                            // Try to parse the number and select the song
                            if (int.TryParse(numberBuffer, out int songNumber))
                            {
                                // Use selection service to handle the number selection
                                if (_selectionService.TrySelectByNumber(songNumber))
                                {
                                    // If selection was successful and we have results, show details
                                    if (currentResults.Count > 0 && _selectionService.SelectedIndex < currentResults.Count)
                                    {
                                        selectedChorus = currentResults[_selectionService.SelectedIndex];
                                        isInDetailView = true;
                                        _selectionService.IsInDetailView = true;
                                        _displayService.DisplayChorusDetail(selectedChorus);
                                    }
                                    else
                                    {
                                        // Just update the display to show the new selection
                                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                                    }
                                    numberBuffer = ""; // Clear buffer after selection
                                }
                            }
                        }
                        else if (key.KeyChar >= 32 && key.KeyChar <= 126) // Printable characters
                        {
                            // Clear number buffer when typing other characters
                            numberBuffer = "";
                            
                            searchString += key.KeyChar;
                            _logger.LogInformation("Added character '{Char}' to search string. Current string: '{SearchString}'", key.KeyChar, searchString);
                            // Let the observer handle the UI update
                            await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                        }
                    }
                    break;
            }
        }
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

                // Special handling for single character searches
                if (searchString.Length == 1)
                {
                    var searchChar = searchString.ToUpperInvariant();
                    
                    // Check if it's a valid musical key
                    if (IsValidMusicalKey(searchChar))
                    {
                        _logger.LogInformation("Single character search '{SearchString}' - treating as key search", searchString);
                        await Task.Delay(searchDelayMs, cts.Token);
                        var keyResults = await _apiClientService.SearchChorusesAsync(searchString, searchIn: "key", cancellationToken: cts.Token);
                        currentResults.Clear();
                        if (keyResults != null)
                            currentResults.AddRange(keyResults);
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        _logger.LogInformation("Key search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);
                    }
                    else
                    {
                        _logger.LogInformation("Single character search '{SearchString}' - treating as text search", searchString);
                        await Task.Delay(searchDelayMs, cts.Token);
                        var textResults = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: cts.Token);
                        currentResults.Clear();
                        if (textResults != null)
                            currentResults.AddRange(textResults);
                        _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        _logger.LogInformation("Text search for '{SearchString}' returned {ResultCount} results", searchString, currentResults.Count);
                    }
                    return;
                }

                // Regular search for 2+ characters
                if (searchString.Length < minSearchLength)
                {
                    _logger.LogInformation("Search string '{SearchString}' is too short (length: {Length}, min: {MinLength})", searchString, searchString.Length, minSearchLength);
                    // Update observer to show the current search string even if it's too short
                    _resultsObserver?.OnResultsChanged(currentResults, searchString);
                    return;
                }

                _logger.LogInformation("Starting search delay for string: '{SearchString}'", searchString);
                await Task.Delay(searchDelayMs, cts.Token);

                _logger.LogInformation("Searching for '{SearchString}' (length: {Length})", searchString, searchString.Length);
                var results = await _apiClientService.SearchChorusesAsync(searchString, cancellationToken: cts.Token);
                currentResults.Clear();
                if (results != null)
                    currentResults.AddRange(results);
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

    private bool IsValidMusicalKey(string key)
    {
        // Valid musical keys: A, B, C, D, E, F, G, and their flat variants (Bb, Ab, Eb, Db, Gb)
        var validKeys = new[] { "A", "B", "C", "D", "E", "F", "G", "BB", "AB", "EB", "DB", "GB" };
        return validKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
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

    private void DrawFrame(int left, int top, int width, int height)
    {
        // Draw top border
        System.Console.SetCursorPosition(left, top);
        System.Console.Write("┌" + new string('─', width - 2) + "┐");
        
        // Draw side borders
        for (int i = 1; i < height - 1; i++)
        {
            System.Console.SetCursorPosition(left, top + i);
            System.Console.Write("│" + new string(' ', width - 2) + "│");
        }
        
        // Draw bottom border
        System.Console.SetCursorPosition(left, top + height - 1);
        System.Console.Write("└" + new string('─', width - 2) + "┘");
    }

    private int GetSongIndexFromMousePosition(int mouseY, int totalResults)
    {
        // Calculate the starting Y position of the results area
        // This depends on the header lines and search prompt lines
        var headerLines = 3; // Title + separator + empty line
        var searchPromptLines = 2; // "Search: " + empty line
        var columnHeaderLines = 2; // Column headers + separator line
        
        var resultsStartY = headerLines + searchPromptLines + columnHeaderLines;
        
        // Calculate which result was clicked
        var resultIndex = mouseY - resultsStartY;
        
        // Ensure the click is within the results area
        if (resultIndex >= 0 && resultIndex < totalResults)
        {
            return resultIndex;
        }
        
        return -1; // Invalid click
    }

    private void ReturnToSearch(ref bool isInDetailView, ref Chorus? selectedChorus, ref string searchString, ref List<Chorus> currentResults)
    {
        isInDetailView = false;
        selectedChorus = null;
        _selectionService.IsInDetailView = false;
        searchString = "";
        currentResults.Clear();
        _resultsObserver?.OnResultsChanged(currentResults, searchString);
    }
} 