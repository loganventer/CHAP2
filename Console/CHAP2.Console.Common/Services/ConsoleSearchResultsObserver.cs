using System;
using System.Collections.Generic;
using CHAP2.Common.Models;
using CHAP2.Console.Common.Interfaces;

namespace CHAP2.Console.Common.Services;

public class ConsoleSearchResultsObserver : ISearchResultsObserver
{
    private readonly int _maxResultsToShow = 10;

    public void OnResultsChanged(List<Chorus> results, string searchTerm)
    {
        // Only clear screen and redraw if we have results or if search term is empty
        if (results != null && results.Count > 0)
        {
            // Clear the entire console and redraw
            System.Console.Clear();
            
            // Draw header
            DrawHeader();
            
            // Draw search prompt at the top
            DrawSearchPrompt(searchTerm);
            
            // Draw results below the prompt
            DrawResults(results, searchTerm);
        }
        else if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Clear screen for empty search
            System.Console.Clear();
            DrawHeader();
            DrawSearchPrompt(searchTerm);
            System.Console.WriteLine("Type to search choruses. Search triggers after each keystroke with delay.");
            System.Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.");
        }
        // For short search terms, don't clear screen - just let the prompt update
    }

    private void DrawHeader()
    {
        System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
        System.Console.WriteLine("=============================================");
        System.Console.WriteLine();
    }

    private void DrawSearchPrompt(string searchTerm)
    {
        System.Console.Write("Search: ");
        System.Console.Write(searchTerm);
        System.Console.WriteLine();
        System.Console.WriteLine();
    }

    private void DrawResults(List<Chorus> results, string searchTerm)
    {
        if (results == null || results.Count == 0)
        {
            System.Console.WriteLine("No results found.");
            return;
        }

        var displayCount = Math.Min(results.Count, _maxResultsToShow);
        
        for (int i = 0; i < displayCount; i++)
        {
            var chorus = results[i];
            System.Console.WriteLine($"{i + 1}. {chorus.Name}");
            
            // Show matching content if search term is provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                DisplayMatchingContent(chorus, searchTerm);
            }
            
            System.Console.WriteLine(); // Add spacing between results
        }
        
        if (results.Count > displayCount)
        {
            System.Console.WriteLine($"... and {results.Count - displayCount} more results");
        }
    }

    private void DisplayMatchingContent(Chorus chorus, string searchTerm)
    {
        bool foundMatch = false;
        
        // Check if search term is in the title
        if (!string.IsNullOrEmpty(chorus.Name) && 
            chorus.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            System.Console.WriteLine($"    Title: {BoldTerm(chorus.Name, searchTerm)}");
            foundMatch = true;
        }
        
        // Check if search term is in the chorus text
        if (!string.IsNullOrEmpty(chorus.ChorusText))
        {
            var textLines = chorus.ChorusText.Split('\n');
            foreach (var line in textLines)
            {
                if (!string.IsNullOrEmpty(line) && 
                    line.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    System.Console.WriteLine($"    Text: {BoldTerm(line.Trim(), searchTerm)}");
                    foundMatch = true;
                }
            }
        }
        
        // If no specific matches found, show a preview of the chorus
        if (!foundMatch && !string.IsNullOrEmpty(chorus.ChorusText))
        {
            var preview = chorus.ChorusText.Split('\n')[0].Trim();
            if (preview.Length > 50)
                preview = preview.Substring(0, 47) + "...";
            System.Console.WriteLine($"    Preview: {preview}");
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
        
        // Use bright red text on bright cyan background with bold formatting for maximum visibility
        // \x1b[1;91;106m = Bold + Bright Red + Bright Cyan Background
        return before + "\x1b[1;91;106m" + match + "\x1b[0m" + after;
    }
} 