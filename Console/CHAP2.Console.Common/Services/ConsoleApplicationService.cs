using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Configuration;
using CHAP2.Domain.Entities;
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
    private static bool _isShowingDialog = false; // Static flag to disable background monitoring during dialogs

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
        
        // Start a background task to monitor window size changes with improved performance
        var windowMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var windowMonitorTask = Task.Run(async () =>
        {
            // Use separate tracking variables for the background task
            var monitorLastWindowSize = (System.Console.WindowWidth, System.Console.WindowHeight);
            var monitorLastBufferSize = (System.Console.BufferWidth, System.Console.BufferHeight);
            var refreshCount = 0;
            const int maxRefreshRate = 10; // Limit refresh rate to prevent excessive updates
            
            while (!windowMonitorCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(500, windowMonitorCts.Token);
                    
                    var currentWindowSize = (System.Console.WindowWidth, System.Console.WindowHeight);
                    var currentBufferSize = (System.Console.BufferWidth, System.Console.BufferHeight);
                    
                    var windowSizeChanged = currentWindowSize != monitorLastWindowSize;
                    var bufferSizeChanged = currentBufferSize != monitorLastBufferSize;
                    
                    // Only refresh on actual window/buffer size changes and limit refresh rate
                    if ((windowSizeChanged || bufferSizeChanged) && refreshCount < maxRefreshRate)
                    {
                        _logger.LogDebug("Background monitor: Display configuration changed - Window: {OldWindow}->{NewWindow}, Buffer: {OldBuffer}->{NewBuffer}", 
                            monitorLastWindowSize, currentWindowSize, monitorLastBufferSize, currentBufferSize);
                        
                        monitorLastWindowSize = currentWindowSize;
                        monitorLastBufferSize = currentBufferSize;
                        refreshCount++;
                        
                        // Force a complete refresh of the display
                        if (isInDetailView && selectedChorus != null)
                        {
                            _logger.LogDebug("Background monitor: Refreshing detail view due to display change");
                            _displayService.DisplayChorusDetail(selectedChorus);
                        }
                        else if (!isInDetailView && !_isShowingDialog)
                        {
                            _logger.LogDebug("Background monitor: Refreshing search results due to display change");
                            _resultsObserver?.ForceRefresh();
                            _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        }
                        else
                        {
                            _logger.LogDebug("Background monitor: Skipping refresh due to dialog or detail view");
                        }
                        
                        // Small delay to ensure the console has stabilized
                        await Task.Delay(50, windowMonitorCts.Token);
                    }
                    else if (!windowSizeChanged && !bufferSizeChanged)
                    {
                        // Reset refresh count when no changes occur
                        refreshCount = 0;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in window monitor task");
                    // Add delay before retrying to prevent rapid error loops
                    await Task.Delay(1000, windowMonitorCts.Token);
                }
            }
        }, windowMonitorCts.Token);

        try
        {
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
                    System.Console.WriteLine("\nConsole input is redirected. Using fallback mode.");
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
                    case ConsoleKey.Escape:
                        if (isInDetailView)
                        {
                            ReturnToSearch(ref isInDetailView, ref selectedChorus, ref searchString, ref currentResults);
                        }
                        else
                        {
                            if (ShowQuitConfirmation())
                            {
                                return;
                            }
                        }
                        break;

                    case ConsoleKey.Backspace:
                        if (!isInDetailView && searchString.Length > 0)
                        {
                            searchString = searchString[..^1];
                            await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                        }
                        break;

                    case ConsoleKey.Enter:
                        if (isInDetailView)
                        {
                            ReturnToSearch(ref isInDetailView, ref selectedChorus, ref searchString, ref currentResults);
                        }
                        else if (currentResults.Count > 0)
                        {
                            var selectedIndex = _selectionService.SelectedIndex;
                            if (selectedIndex >= 0 && selectedIndex < currentResults.Count)
                            {
                                selectedChorus = currentResults[selectedIndex];
                                isInDetailView = true;
                                _selectionService.IsInDetailView = true;
                                _displayService.DisplayChorusDetail(selectedChorus);
                            }
                        }
                        break;

                    case ConsoleKey.DownArrow:
                        if (!isInDetailView && currentResults.Count > 0)
                        {
                            _selectionService.UpdateTotalItems(currentResults.Count);
                            _selectionService.MoveDown();
                            _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        }
                        break;

                    case ConsoleKey.UpArrow:
                        if (!isInDetailView && currentResults.Count > 0)
                        {
                            _selectionService.MoveUp();
                            _resultsObserver?.OnResultsChanged(currentResults, searchString);
                        }
                        break;

                    case ConsoleKey.D0:
                    case ConsoleKey.D1:
                    case ConsoleKey.D2:
                    case ConsoleKey.D3:
                    case ConsoleKey.D4:
                    case ConsoleKey.D5:
                    case ConsoleKey.D6:
                    case ConsoleKey.D7:
                    case ConsoleKey.D8:
                    case ConsoleKey.D9:
                        if (!isInDetailView && currentResults.Count > 0)
                        {
                            var number = (int)key.Key - (int)ConsoleKey.D0;
                            var currentTime = DateTime.Now;
                            
                            if (currentTime - lastNumberKeyTime < TimeSpan.FromMilliseconds(500))
                            {
                                numberBuffer += number.ToString();
                            }
                            else
                            {
                                numberBuffer = number.ToString();
                            }
                            
                            lastNumberKeyTime = currentTime;
                            
                            if (int.TryParse(numberBuffer, out var index) && index > 0 && index <= currentResults.Count)
                            {
                                _selectionService.UpdateTotalItems(currentResults.Count);
                                _selectionService.SelectedIndex = index - 1;
                                _resultsObserver?.OnResultsChanged(currentResults, searchString);
                            }
                        }
                        break;

                    default:
                        if (!isInDetailView && key.KeyChar >= 32 && key.KeyChar <= 126)
                        {
                            searchString += key.KeyChar;
                            await ProcessSearchString(searchString, searchDelayMs, minSearchLength, currentResults, searchCancellationTokenSource, cts, lastSearchTask, cancellationToken);
                        }
                        break;
                }
            }
        }
        finally
        {
            // Cleanup
            windowMonitorCts.Cancel();
            try
            {
                await windowMonitorTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            
            System.Console.CursorVisible = true;
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
                {
                    var lowerSearch = searchString.ToLowerInvariant();
                    var sorted = results.OrderByDescending(r =>
                        (!string.IsNullOrEmpty(r.Name) && r.Name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0) ? 2 :
                        (!string.IsNullOrEmpty(r.ChorusText) && r.ChorusText.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0) ? 1 : 0
                    ).ThenBy(r => r.Name).ToList();
                    currentResults.AddRange(sorted);
                }
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
        _logger.LogInformation("Returning to search view from detail view");
        
        isInDetailView = false;
        selectedChorus = null;
        _selectionService.IsInDetailView = false;
        searchString = "";
        currentResults.Clear();
        
        // Reset selection service for fresh start
        _selectionService.UpdateTotalItems(0);
        _selectionService.ResetSelection();
        
        _logger.LogInformation("Redrawing search view with empty results and search string");
        _logger.LogInformation("Observer is null: {ObserverIsNull}", _resultsObserver == null);
        
        // Force a complete refresh
        _resultsObserver?.ForceRefresh();
        _resultsObserver?.OnResultsChanged(currentResults, searchString);
        
        _logger.LogInformation("Search view redraw completed");
        
        // Small delay to let the display settle
        Thread.Sleep(100);
    }

    private bool ShowQuitConfirmation()
    {
        // Set dialog flag to prevent background monitor interference
        _isShowingDialog = true;
        
        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        
        // Calculate center position for the dialog
        var dialogWidth = 40;
        var dialogHeight = 5;
        var left = (windowWidth - dialogWidth) / 2;
        var top = (windowHeight - dialogHeight) / 2;
        
        // Clear the screen
        System.Console.Clear();
        
        // Draw the bordered dialog
        DrawFrame(left, top, dialogWidth, dialogHeight);
        
        // Add the confirmation text
        var message = "Are you sure you want to quit? (Y/N)";
        var messageLeft = left + (dialogWidth - message.Length) / 2;
        System.Console.SetCursorPosition(messageLeft, top + 2);
        System.Console.Write(message);
        
        // Position cursor for input
        System.Console.SetCursorPosition(left + dialogWidth / 2, top + 3);
        
        // Small delay to prevent background interference
        Thread.Sleep(200);
        
        // Wait for key input
        var key = System.Console.ReadKey(true);
        
        // Clear dialog flag
        _isShowingDialog = false;
        
        return key.KeyChar == 'Y' || key.KeyChar == 'y';
    }

    public void ClearScreenWithDelay(string message = "Goodbye!")
    {
        // Clear the screen
        System.Console.Clear();
        
        // Center the message on screen
        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        var left = (windowWidth - message.Length) / 2;
        var top = windowHeight / 2;
        
        // Position cursor and write message
        System.Console.SetCursorPosition(left, top);
        System.Console.Write(message);
        
        // Wait 500ms before continuing
        Thread.Sleep(500);
        
        // Clear the screen again
        System.Console.Clear();
    }
} 