using CHAP2.Console.Common.Interfaces;
using CHAP2.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Common.Services;

public class ConsoleDisplayService : IConsoleDisplayService
{
    private readonly ILogger<ConsoleDisplayService> _logger;

    public ConsoleDisplayService(ILogger<ConsoleDisplayService> logger)
    {
        _logger = logger;
    }

    public void DisplayChorus(Chorus? chorus)
    {
        if (chorus == null)
        {
            WriteLine("No chorus to display.");
            return;
        }

        WriteLine($"Title: {chorus.Name}");
        WriteLine($"Key: {chorus.Key}");
        WriteLine($"Text: {chorus.ChorusText}");
    }

    public void DisplayChorusDetail(Chorus? chorus)
    {
        if (chorus == null)
        {
            WriteLine("No chorus to display.");
            return;
        }

        // Clear screen
        ClearScreen();
        
        var windowWidth = System.Console.WindowWidth;
        var windowHeight = System.Console.WindowHeight;
        
        // Calculate starting position to center content vertically
        var lines = new List<string>();
        
        // Add title (centered)
        var title = chorus.Name ?? "Untitled";
        var titlePadding = Math.Max(0, (windowWidth - title.Length) / 2);
        lines.Add(new string(' ', titlePadding) + title);
        lines.Add(""); // Extra spacing after title
        
        // Add key (centered)
        var keyInfo = $"Key: {chorus.Key}";
        var keyPadding = Math.Max(0, (windowWidth - keyInfo.Length) / 2);
        lines.Add(new string(' ', keyPadding) + keyInfo);
        lines.Add(""); // Extra spacing for appearance
        
        // Add blank line
        lines.Add("");
        
        // Add chorus text lines (normalized to fit screen)
        var text = chorus.ChorusText ?? "";
        var normalizedLines = NormalizeTextToScreen(text, windowWidth, windowHeight);
        
        foreach (var line in normalizedLines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var linePadding = Math.Max(0, (windowWidth - line.Length) / 2);
                lines.Add(new string(' ', linePadding) + line);
            }
        }
        
        // Calculate vertical centering
        var totalLines = lines.Count;
        var startRow = Math.Max(0, (windowHeight - totalLines) / 2);
        
        // Display content
        SetCursorPosition(0, startRow);
        foreach (var line in lines)
        {
            WriteLine(line);
        }
        
        // Add navigation instructions at the bottom
        var instructions = "Press Escape to return to search";
        var instructionPadding = Math.Max(0, (windowWidth - instructions.Length) / 2);
        SetCursorPosition(instructionPadding, windowHeight - 3);
        WriteLine(" "); // Extra spacing
        SetCursorPosition(instructionPadding, windowHeight - 2);
        WriteLine(instructions);
    }

    public void DisplayChoruses(List<Chorus>? choruses)
    {
        if (choruses == null)
        {
            WriteLine("(null chorus list)");
            return;
        }
        for (int i = 0; i < choruses.Count; i++)
        {
            WriteLine($"--- Chorus {i + 1} ---");
            DisplayChorus(choruses[i]);
            WriteLine(" ");
        }
    }

    public void ClearScreen()
    {
        System.Console.Clear();
    }

    public void SetCursorPosition(int left, int top)
    {
        System.Console.SetCursorPosition(left, top);
    }

    public void WriteLine(string text)
    {
        System.Console.WriteLine(text);
    }

    public void Write(string text)
    {
        System.Console.Write(text);
    }

    private List<string> NormalizeTextToScreen(string text, int windowWidth, int windowHeight)
    {
        // Calculate available space for text
        // Reserve space for: header (3 lines), title (3 lines), key (2 lines), blank line (1 line), 
        // spacing around each text line (2 lines per text line), instructions (2 lines)
        var reservedLines = 3 + 3 + 2 + 1 + 2; // Base reserved lines
        var availableLines = windowHeight - reservedLines - 4; // 4 for safety margin
        
        // Calculate max characters per line (accounting for centering padding)
        var maxCharsPerLine = windowWidth - 4; // 4 for safety margin
        
        // Split text into original lines and clean them
        var originalLines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        
        foreach (var originalLine in originalLines)
        {
            var cleanedLine = originalLine.Replace("\t", " ")
                                        .Replace("  ", " ") // Replace double spaces with single
                                        .Trim();
            
            if (string.IsNullOrEmpty(cleanedLine))
                continue; // Skip blank lines
            
            // If the line fits within maxCharsPerLine, add it as-is
            if (cleanedLine.Length <= maxCharsPerLine)
            {
                lines.Add(cleanedLine);
            }
            else
            {
                // Line is too long, word-wrap it
                var words = cleanedLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var currentLine = "";
                
                foreach (var word in words)
                {
                    // If adding this word would exceed the line length
                    if (currentLine.Length + word.Length + 1 > maxCharsPerLine)
                    {
                        if (currentLine.Length > 0)
                        {
                            lines.Add(currentLine.Trim());
                            currentLine = word;
                        }
                        else
                        {
                            // Word is too long, truncate it
                            lines.Add(word.Substring(0, Math.Min(maxCharsPerLine, word.Length)));
                        }
                    }
                    else
                    {
                        currentLine += (currentLine.Length > 0 ? " " : "") + word;
                    }
                }
                
                // Add the last line if it's not empty
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine.Trim());
                }
            }
            
            // Check if we've reached the maximum number of lines
            if (lines.Count >= availableLines)
            {
                // Add ellipsis to the last line if it's not empty
                if (lines.Count > 0 && !string.IsNullOrEmpty(lines[lines.Count - 1]))
                {
                    var lastLine = lines[lines.Count - 1];
                    if (lastLine.Length + 3 <= maxCharsPerLine)
                    {
                        lines[lines.Count - 1] = lastLine + "...";
                    }
                    else
                    {
                        // Replace the last few characters with ellipsis
                        var truncatedLength = Math.Max(0, maxCharsPerLine - 3);
                        lines[lines.Count - 1] = lastLine.Substring(0, truncatedLength) + "...";
                    }
                }
                break;
            }
        }
        
        return lines;
    }
} 