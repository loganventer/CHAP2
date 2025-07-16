using System;
using System.Collections.Generic;
using System.Linq;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Configuration;
using Microsoft.Extensions.Options;

namespace CHAP2.Console.Common.Services;

public class ConsoleSearchResultsObserver : ISearchResultsObserver
{
    private readonly ConsoleDisplaySettings _settings;
    private readonly ISelectionService _selectionService;
    private int _currentWindowStartIndex = 0;
    private (int width, int height) _lastWindowSize;
    private (int width, int height) _lastBufferSize;
    private int _lastCursorTop;
    private int _lastCursorLeft;

    public ConsoleSearchResultsObserver(IOptions<ConsoleDisplaySettings> settings, ISelectionService selectionService)
    {
        _settings = settings.Value;
        _selectionService = selectionService;
        _lastWindowSize = (System.Console.WindowWidth, System.Console.WindowHeight);
        _lastBufferSize = (System.Console.BufferWidth, System.Console.BufferHeight);
        _lastCursorTop = System.Console.CursorTop;
        _lastCursorLeft = System.Console.CursorLeft;
    }

    public void OnResultsChanged(List<Chorus> results, string searchTerm)
    {
        // Check if window size or buffer size has changed (indicating font size change)
        var currentWindowSize = (System.Console.WindowWidth, System.Console.WindowHeight);
        var currentBufferSize = (System.Console.BufferWidth, System.Console.BufferHeight);
        var windowSizeChanged = _lastWindowSize != currentWindowSize;
        var bufferSizeChanged = currentBufferSize != _lastBufferSize;
        
        // Also check if the effective display area has changed (font size changes)
        var effectiveDisplayChanged = HasEffectiveDisplayChanged();
        
        if (windowSizeChanged || bufferSizeChanged || effectiveDisplayChanged)
        {
            // Window, buffer, or effective display changed, clear screen and reset window position
            System.Console.Clear();
            _currentWindowStartIndex = 0;
            _lastWindowSize = currentWindowSize;
            _lastBufferSize = currentBufferSize;
        }
        else
        {
            // Clear screen and redraw for any search term (including short ones)
            System.Console.Clear();
            
            // Reset window start index when results change
            _currentWindowStartIndex = 0;
        }
        
        // Draw header
        DrawHeader();
        
        // Draw search prompt at the top
        DrawSearchPrompt(searchTerm);
        
        // Update selection service with new results
        _selectionService.UpdateTotalItems(results?.Count ?? 0);
        
        // Calculate how many results can fit on the screen
        var availableHeight = CalculateAvailableHeight();
        var maxResultsToShow = Math.Min(results?.Count ?? 0, availableHeight);
        
        // If we have results, draw them
        if (results != null && results.Count > 0)
        {
            DrawResults(results, searchTerm, maxResultsToShow);
        }
        else if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Show instructions for empty search
            System.Console.WriteLine(_settings.InstructionsText);
            System.Console.WriteLine(_settings.InstructionsControls);
        }
        else if (searchTerm.Length == 1)
        {
            // For single character searches, show a message about key search
            System.Console.WriteLine("Searching by musical key...");
            if (results != null && results.Count == 0)
            {
                System.Console.WriteLine("No choruses found in this key.");
            }
        }
        else
        {
            // Show message for short search terms (only for regular searches, not key searches)
            System.Console.WriteLine(string.Format(_settings.ShortSearchText, searchTerm.Length));
        }
        
        // Position cursor at the end of the search prompt for proper caret positioning
        PositionCursorAtSearchPrompt(searchTerm);
    }

    private int CalculateAvailableHeight()
    {
        var windowHeight = System.Console.WindowHeight;
        var reservedLines = _settings.HeaderLines + _settings.SearchPromptLines + _settings.InstructionLines;
        
        // Account for column headers (2 lines) and potential scroll indicators (1 line)
        var extraReservedLines = 3;
        
        // Calculate available lines for results
        var availableLines = windowHeight - reservedLines - _settings.SafetyMargin - extraReservedLines;
        
        // Ensure we show at least the minimum number of results if possible, but never exceed available space
        return Math.Max(_settings.MinResultsToShow, Math.Max(0, availableLines));
    }

    private (int startIndex, int displayCount) CalculateScrollWindow(int totalResults, int maxResultsToShow)
    {
        if (totalResults <= maxResultsToShow)
        {
            // All results fit in the window, no scrolling needed
            return (0, totalResults);
        }

        var selectedIndex = _selectionService.SelectedIndex;
        
        // Ensure selected index is within bounds
        if (selectedIndex < 0) selectedIndex = 0;
        if (selectedIndex >= totalResults) selectedIndex = totalResults - 1;
        
        // Get the current window start index (we'll need to track this)
        var currentStartIndex = GetCurrentWindowStartIndex();
        
        // Check if the selected item is within the current window
        var isInCurrentWindow = selectedIndex >= currentStartIndex && 
                               selectedIndex < currentStartIndex + maxResultsToShow;
        
        if (isInCurrentWindow)
        {
            // Selection is within current window, don't scroll
            var displayCount = Math.Min(maxResultsToShow, totalResults - currentStartIndex);
            return (currentStartIndex, displayCount);
        }
        else
        {
            // Selection is outside current window, need to scroll
            int newStartIndex;
            
            if (selectedIndex < currentStartIndex)
            {
                // Selection is above current window, scroll up
                // Position selected item at the top of the window
                newStartIndex = selectedIndex;
            }
            else
            {
                // Selection is below current window, scroll down
                // Position selected item at the bottom of the window
                newStartIndex = selectedIndex - maxResultsToShow + 1;
            }
            
            // Ensure we don't go past the beginning
            if (newStartIndex < 0)
            {
                newStartIndex = 0;
            }
            
            // Ensure we don't go past the end
            if (newStartIndex + maxResultsToShow > totalResults)
            {
                newStartIndex = totalResults - maxResultsToShow;
            }
            
            // Update the window start index
            SetCurrentWindowStartIndex(newStartIndex);
            
            var displayCount = Math.Min(maxResultsToShow, totalResults - newStartIndex);
            return (newStartIndex, displayCount);
        }
    }

    private void DrawHeader()
    {
        System.Console.WriteLine(_settings.HeaderTitle);
        System.Console.WriteLine(_settings.HeaderSeparator);
        System.Console.WriteLine();
    }

    private void DrawSearchPrompt(string searchTerm)
    {
        System.Console.Write(_settings.SearchPromptText);
        
        // Get console window width to handle long search terms
        var windowWidth = System.Console.WindowWidth;
        var maxSearchTermLength = windowWidth - _settings.SearchPromptPrefixLength - _settings.SafetyMargin;
        if (maxSearchTermLength < 0) maxSearchTermLength = 0;
        
        if (searchTerm.Length > maxSearchTermLength)
        {
            // Truncate the search term for display but keep the full term for functionality
            var substringLength = maxSearchTermLength - _settings.TruncationSuffixLength;
            if (substringLength < 0) substringLength = 0;
            var displayTerm = searchTerm.Substring(0, substringLength) + _settings.TruncationSuffix;
            System.Console.Write(displayTerm);
        }
        else
        {
            System.Console.Write(searchTerm);
        }
        
        System.Console.WriteLine();
        System.Console.WriteLine();
    }

    private void PositionCursorAtSearchPrompt(string searchTerm)
    {
        // Calculate the position where the cursor should be after the search prompt
        var windowWidth = System.Console.WindowWidth;
        var maxSearchTermLength = windowWidth - _settings.SearchPromptPrefixLength - _settings.SafetyMargin;
        
        // Calculate the actual displayed search term length
        var displayedSearchTermLength = searchTerm.Length > maxSearchTermLength 
            ? maxSearchTermLength - _settings.TruncationSuffixLength + _settings.TruncationSuffixLength
            : searchTerm.Length;
            
        var cursorLeft = _settings.SearchPromptPrefixLength + displayedSearchTermLength;
        var cursorTop = _settings.HeaderLines;
        
        // Get current console window dimensions
        var windowHeight = System.Console.WindowHeight;
        
        // Ensure cursor position is within console bounds
        if (cursorLeft >= windowWidth)
        {
            cursorLeft = windowWidth - 1;
        }
        
        if (cursorTop >= windowHeight)
        {
            cursorTop = windowHeight - 1;
        }
        
        // Ensure minimum bounds
        if (cursorLeft < 0) cursorLeft = 0;
        if (cursorTop < 0) cursorTop = 0;
        
        System.Console.SetCursorPosition(cursorLeft, cursorTop);
    }

    private void DrawResults(List<Chorus> results, string searchTerm, int maxResultsToShow)
    {
        if (results == null || results.Count == 0)
        {
            System.Console.WriteLine(_settings.NoResultsText);
            return;
        }

        var windowWidth = System.Console.WindowWidth;
        
        // Calculate scroll window based on selected index
        var (startIndex, displayCount) = CalculateScrollWindow(results.Count, maxResultsToShow);
        
        // Calculate the maximum width needed for result numbers (e.g., "50. " = 4 characters)
        var maxNumberWidth = displayCount.ToString().Length + 2; // +2 for ". "
        
        // Calculate column widths with better proportions, accounting for the number prefix
        // Title gets 40% of available width, Key gets 15%, Context gets 45%
        var availableWidth = windowWidth - maxNumberWidth - (_settings.ColumnSeparator.Length * 2);
        var titleColumnWidth = Math.Max(_settings.MinTitleColumnWidth, (int)(availableWidth * 0.4));
        var keyColumnWidth = Math.Max(_settings.MinKeyColumnWidth, (int)(availableWidth * 0.15));
        var contextColumnWidth = availableWidth - titleColumnWidth - keyColumnWidth;
        
        // Draw headers with proper alignment
        var headerNumber = "".PadRight(maxNumberWidth);
        var titleHeader = "Title".PadRight(titleColumnWidth);
        var keyHeader = "Key".PadLeft(keyColumnWidth / 2).PadRight(keyColumnWidth); // Center the key header
        var contextHeader = "Context".PadRight(contextColumnWidth);
        
        System.Console.WriteLine($"{headerNumber}{titleHeader}{_settings.ColumnSeparator}{keyHeader}{_settings.ColumnSeparator}{contextHeader}");
        
        // Draw separator line
        var separatorLine = new string('-', windowWidth);
        System.Console.WriteLine(separatorLine);
        
        // Draw results with selection highlighting
        for (int i = 0; i < displayCount; i++)
        {
            var actualIndex = startIndex + i;
            var result = results[actualIndex];
            var number = $"{actualIndex + 1}.".PadRight(maxNumberWidth);
            
            var title = GetTitleDisplay(result.Name, titleColumnWidth);
            var key = GetKeyDisplay(result.Key, keyColumnWidth);
            var context = GetContextFromText(result.ChorusText, searchTerm, contextColumnWidth);
            
            // Highlight selected row
            var isSelected = actualIndex == _selectionService.SelectedIndex && !_selectionService.IsInDetailView;
            
            if (isSelected)
            {
                SetSelectionColors();
            }
            
            // Ensure the entire line doesn't exceed window width
            var fullLine = $"{number}{title}{_settings.ColumnSeparator}{key}{_settings.ColumnSeparator}{context}";
            if (fullLine.Length > windowWidth)
            {
                // Truncate context further if needed
                var maxContextLength = windowWidth - number.Length - title.Length - key.Length - (_settings.ColumnSeparator.Length * 2);
                context = GetContextFromText(result.ChorusText, searchTerm, Math.Max(0, maxContextLength));
            }
            
            System.Console.WriteLine($"{number}{title}{_settings.ColumnSeparator}{key}{_settings.ColumnSeparator}{context}");
            
            if (isSelected)
            {
                System.Console.ResetColor();
            }
        }
        
        // Show scroll indicators with exact counts
        var itemsAbove = startIndex;
        var itemsBelow = results.Count - (startIndex + displayCount);
        
        if (itemsAbove > 0 && itemsBelow > 0)
        {
            // Items above and below
            System.Console.WriteLine($"↑ {itemsAbove} above | ↓ {itemsBelow} below | Showing {startIndex + 1}-{startIndex + displayCount} of {results.Count}");
        }
        else if (itemsAbove > 0)
        {
            // Only items above
            System.Console.WriteLine($"↑ {itemsAbove} above | Showing {startIndex + 1}-{startIndex + displayCount} of {results.Count}");
        }
        else if (itemsBelow > 0)
        {
            // Only items below
            System.Console.WriteLine($"↓ {itemsBelow} below | Showing {startIndex + 1}-{startIndex + displayCount} of {results.Count}");
        }
    }

    private string GetTitleDisplay(string title, int maxWidth)
    {
        if (string.IsNullOrEmpty(title))
            return "".PadRight(maxWidth);
            
        var titleString = title;
        if (titleString.Length > maxWidth)
        {
            titleString = titleString.Substring(0, maxWidth - _settings.TruncationSuffixLength) + _settings.TruncationSuffix;
        }
        
        return titleString.PadRight(maxWidth);
    }

    private string GetKeyDisplay(MusicalKey key, int maxWidth)
    {
        var keyString = key.ToString();
        if (key == MusicalKey.NotSet)
        {
            keyString = "Not Set";
        }
        
        // Truncate if too long
        if (keyString.Length > maxWidth)
        {
            keyString = keyString.Substring(0, maxWidth - _settings.TruncationSuffixLength) + _settings.TruncationSuffix;
        }
        
        return keyString.PadRight(maxWidth);
    }

    private string GetContextFromText(string text, string searchTerm, int maxWidth)
    {
        var index = text.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return "";
        
        // Remove any newlines to prevent wrapping
        text = text.Replace("\r", "").Replace("\n", " ").Replace("\t", " ");
        
        // Calculate start and end positions for total context characters around the search term
        var remainingChars = _settings.TotalContextCharacters - searchTerm.Length;
        var charsBefore = remainingChars / 2;
        var charsAfter = remainingChars - charsBefore; // Handle odd numbers
        
        var startPos = Math.Max(0, index - charsBefore);
        var endPos = Math.Min(text.Length, index + searchTerm.Length + charsAfter);
        
        var context = text.Substring(startPos, endPos - startPos);
        
        // Add ellipses if we're not at the beginning or end
        if (startPos > 0)
        {
            context = _settings.LeftEllipsis + context;
        }
        if (endPos < text.Length)
        {
            context = context + _settings.RightEllipsis;
        }
        
        // Highlight the search term
        context = BoldTerm(context, searchTerm);
        
        // Ensure it fits within maxWidth
        if (context.Length > maxWidth)
        {
            context = context.Substring(0, maxWidth - _settings.TruncationSuffixLength) + _settings.TruncationSuffix;
        }
        
        return context.PadRight(maxWidth);
    }

    private string BoldTerm(string input, string term)
    {
        if (string.IsNullOrEmpty(term)) return input;
        
        var index = input.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return input;
        
        return input.Substring(0, index) + 
               "\x1b[1;31m" + input.Substring(index, term.Length) + "\x1b[0m" + 
               input.Substring(index + term.Length);
    }

    private void SetSelectionColors()
    {
        // Store current colors
        var originalBackground = System.Console.BackgroundColor;
        var originalForeground = System.Console.ForegroundColor;
        
        // Use contrasting colors based on current theme
        if (IsDarkBackground(originalBackground))
        {
            // Dark background - use light colors
            System.Console.BackgroundColor = ConsoleColor.White;
            System.Console.ForegroundColor = ConsoleColor.Black;
        }
        else
        {
            // Light background - use dark colors
            System.Console.BackgroundColor = ConsoleColor.Black;
            System.Console.ForegroundColor = ConsoleColor.White;
        }
    }

    private bool IsDarkBackground(ConsoleColor backgroundColor)
    {
        return backgroundColor == ConsoleColor.Black || 
               backgroundColor == ConsoleColor.DarkBlue || 
               backgroundColor == ConsoleColor.DarkGreen || 
               backgroundColor == ConsoleColor.DarkCyan || 
               backgroundColor == ConsoleColor.DarkRed || 
               backgroundColor == ConsoleColor.DarkMagenta || 
               backgroundColor == ConsoleColor.DarkYellow || 
               backgroundColor == ConsoleColor.Gray;
    }

    private int GetCurrentWindowStartIndex()
    {
        return _currentWindowStartIndex;
    }

    private void SetCurrentWindowStartIndex(int startIndex)
    {
        _currentWindowStartIndex = startIndex;
    }

    public void ForceRefresh()
    {
        // Force a complete refresh by clearing the screen and resetting all tracking
        System.Console.Clear();
        _lastWindowSize = (System.Console.WindowWidth, System.Console.WindowHeight);
        _lastBufferSize = (System.Console.BufferWidth, System.Console.BufferHeight);
        _currentWindowStartIndex = 0;
        
        // Small delay to ensure console has stabilized after font size changes
        Thread.Sleep(10);
    }
    
    private bool HasEffectiveDisplayChanged()
    {
        // Check if the cursor position has moved significantly, which can indicate font size changes
        var currentCursorTop = System.Console.CursorTop;
        var currentCursorLeft = System.Console.CursorLeft;
        
        // If cursor position has changed significantly, it might indicate a font size change
        // This is a heuristic approach since .NET doesn't provide direct font size information
        var cursorMovedSignificantly = Math.Abs(currentCursorTop - _lastCursorTop) > 2 || 
                                     Math.Abs(currentCursorLeft - _lastCursorLeft) > 2;
        
        // Update last cursor position
        _lastCursorTop = currentCursorTop;
        _lastCursorLeft = currentCursorLeft;
        
        return cursorMovedSignificantly;
    }
} 