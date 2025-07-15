using System;
using System.Text;
using System.Text.RegularExpressions;
using CHAP2.Common.Configuration;
using CHAP2.Common.Interfaces;

namespace CHAP2.Common.Services;

public class TextStandardizationService
{
    private readonly TextStandardizationSettings _settings;

    public TextStandardizationService(IConfigurationService configService)
    {
        _settings = configService.GetConfiguration<TextStandardizationSettings>("TextStandardizationSettings");
    }

    public string StandardizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return title;

        // First, apply title case (first letter of each word capitalized, rest lowercase)
        var titleCase = ApplyTitleCase(title);
        
        // Then, ensure religious titles are properly capitalized
        return CapitalizeReligiousTitles(titleCase);
    }

    public string StandardizeChorusText(string chorusText)
    {
        if (string.IsNullOrWhiteSpace(chorusText))
            return chorusText;

        // Split into lines and process each line
        var lines = chorusText.Split('\n');
        var standardizedLines = new string[lines.Length];

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!string.IsNullOrWhiteSpace(line))
            {
                // Capitalize religious titles in each line
                standardizedLines[i] = CapitalizeReligiousTitles(line);
            }
            else
            {
                standardizedLines[i] = line;
            }
        }

        return string.Join("\n", standardizedLines);
    }

    private string ApplyTitleCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var words = text.Split(' ');
        var titleCaseWords = new string[words.Length];

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (!string.IsNullOrWhiteSpace(word))
            {
                // Handle words with special characters (like hyphens)
                if (word.Contains('-'))
                {
                    var parts = word.Split('-');
                    var titleCaseParts = new string[parts.Length];
                    for (int j = 0; j < parts.Length; j++)
                    {
                        titleCaseParts[j] = CapitalizeFirstLetter(parts[j]);
                    }
                    titleCaseWords[i] = string.Join("-", titleCaseParts);
                }
                else
                {
                    titleCaseWords[i] = CapitalizeFirstLetter(word);
                }
            }
            else
            {
                titleCaseWords[i] = word;
            }
        }

        return string.Join(" ", titleCaseWords);
    }

    private string CapitalizeFirstLetter(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
            return word;

        if (word.Length == 1)
            return word.ToUpper();

        return char.ToUpper(word[0]) + word.Substring(1).ToLower();
    }

    private string CapitalizeReligiousTitles(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var result = text;

        foreach (var title in _settings.ReligiousTitles)
        {
            // Use case-insensitive regex to find and replace religious titles
            var pattern = $@"\b{Regex.Escape(title)}\b";
            result = Regex.Replace(result, pattern, title, RegexOptions.IgnoreCase);
        }

        return result;
    }
} 