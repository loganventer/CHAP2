using System;
using System.Collections.Generic;
using CHAP2.Common.Models;
using CHAP2.Console.Common.Interfaces;

namespace CHAP2.Console.Common.Services;

public class ConsoleSearchResultsObserver : ISearchResultsObserver
{
    private int _lastResultsLineCount = 0;

    public void OnResultsChanged(List<Chorus> results, string searchTerm)
    {
        // Save current cursor position
        int origLeft = Console.CursorLeft;
        int origTop = Console.CursorTop;

        // Always print the prompt at the top
        Console.SetCursorPosition(0, 0);
        Console.Write($"Search: {searchTerm}");
        Console.Write(new string(' ', Console.WindowWidth - (8 + searchTerm.Length))); // Clear to end of line

        // Results start at line 1
        Console.SetCursorPosition(0, 1);

        // Clear previous results area
        for (int i = 0; i < _lastResultsLineCount; i++)
        {
            Console.Write(new string(' ', Console.WindowWidth - 1));
            if (i < _lastResultsLineCount - 1) Console.WriteLine();
        }
        // Move back to start of results area
        Console.SetCursorPosition(0, 1);

        // Draw new results
        int linesWritten = 0;
        if (results != null && results.Count > 0)
        {
            var displayCount = Math.Min(results.Count, 10); // Show up to 10 results
            for (int i = 0; i < displayCount; i++)
            {
                var chorus = results[i];
                Console.WriteLine($"{i + 1}.");
                linesWritten++;
                linesWritten += DisplaySearchResult(chorus, searchTerm);
            }
            if (results.Count > displayCount)
            {
                Console.WriteLine($"  ... and {results.Count - displayCount} more");
                linesWritten++;
            }
        }
        _lastResultsLineCount = linesWritten;

        // Restore cursor to prompt line, after the search term
        Console.SetCursorPosition(8 + searchTerm.Length, 0);
    }

    private int DisplaySearchResult(Chorus chorus, string searchTerm)
    {
        int lines = 0;
        bool found = false;
        // Title
        if (!string.IsNullOrEmpty(chorus.Name) && !string.IsNullOrEmpty(searchTerm) &&
            chorus.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Console.WriteLine($"    Title: {BoldTerm(chorus.Name, searchTerm)}");
            lines++;
            found = true;
        }
        // ChorusText
        if (!string.IsNullOrEmpty(chorus.ChorusText) && !string.IsNullOrEmpty(searchTerm))
        {
            var textLines = chorus.ChorusText.Split('\n');
            foreach (var line in textLines)
            {
                if (line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Console.WriteLine($"    Text: {BoldTerm(line, searchTerm)}");
                    lines++;
                    found = true;
                }
            }
        }
        if (!found)
        {
            Console.WriteLine($"    Title: {chorus.Name}");
            lines++;
        }
        return lines;
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
} 