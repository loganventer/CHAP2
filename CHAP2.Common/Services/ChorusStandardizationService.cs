using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;
using CHAP2.Common.Enum;
using CHAP2.Common.Configuration;

namespace CHAP2.Common.Services;

public class ChorusStandardizationService
{
    private readonly IChorusResource _chorusResource;
    private readonly TextStandardizationService _textStandardizationService;

    public ChorusStandardizationService(IChorusResource chorusResource, IConfigurationService configService)
    {
        _chorusResource = chorusResource;
        _textStandardizationService = new TextStandardizationService(configService);
    }

    public async Task<int> StandardizeAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        var allChoruses = await _chorusResource.GetAllChorusesAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var chorus in allChoruses)
        {
            var wasUpdated = false;
            var originalName = chorus.Name;
            var originalText = chorus.ChorusText;
            var originalKey = chorus.Key;

            // Standardize title
            var standardizedName = _textStandardizationService.StandardizeTitle(chorus.Name);
            if (standardizedName != originalName)
            {
                chorus.Name = standardizedName;
                wasUpdated = true;
            }

            // Standardize text
            var standardizedText = _textStandardizationService.StandardizeChorusText(chorus.ChorusText);
            if (standardizedText != originalText)
            {
                chorus.ChorusText = standardizedText;
                wasUpdated = true;
            }

            // Try to detect key if not already set
            if (chorus.Key == MusicalKey.NotSet)
            {
                var detectedKey = ExtractMusicalKeyFromTitle(standardizedName) ?? 
                                ExtractMusicalKeyFromText(standardizedText);
                if (detectedKey.HasValue && detectedKey.Value != MusicalKey.NotSet)
                {
                    chorus.Key = detectedKey.Value;
                    wasUpdated = true;
                }
            }

            if (wasUpdated)
            {
                await _chorusResource.UpdateChorusAsync(chorus, cancellationToken);
                updatedCount++;
            }
        }

        return updatedCount;
    }

    public async Task<bool> StandardizeChorusAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        var wasUpdated = false;
        var originalName = chorus.Name;
        var originalText = chorus.ChorusText;
        var originalKey = chorus.Key;

        // Standardize title
        var standardizedName = _textStandardizationService.StandardizeTitle(chorus.Name);
        if (standardizedName != originalName)
        {
            chorus.Name = standardizedName;
            wasUpdated = true;
        }

        // Standardize text
        var standardizedText = _textStandardizationService.StandardizeChorusText(chorus.ChorusText);
        if (standardizedText != originalText)
        {
            chorus.ChorusText = standardizedText;
            wasUpdated = true;
        }

        // Try to detect key if not already set
        if (chorus.Key == MusicalKey.NotSet)
        {
            var detectedKey = ExtractMusicalKeyFromTitle(standardizedName) ?? 
                            ExtractMusicalKeyFromText(standardizedText);
            if (detectedKey.HasValue && detectedKey.Value != MusicalKey.NotSet)
            {
                chorus.Key = detectedKey.Value;
                wasUpdated = true;
            }
        }

        if (wasUpdated)
        {
            await _chorusResource.UpdateChorusAsync(chorus, cancellationToken);
        }

        return wasUpdated;
    }

    private static MusicalKey? ExtractMusicalKeyFromTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        // Look for keys in brackets (e.g., "Song Title (C)", "Song Title (G)")
        var bracketPattern = @"\(([A-G][#b]?)\)";
        var bracketMatch = System.Text.RegularExpressions.Regex.Match(title, bracketPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (bracketMatch.Success)
        {
            var keyString = bracketMatch.Groups[1].Value;
            if (TryParseMusicalKey(keyString, out var key))
            {
                return key;
            }
        }

        // Look for standalone keys (e.g., "Song Title C", "Song Title G")
        var standalonePattern = @"\b([A-G][#b]?)\b";
        var standaloneMatch = System.Text.RegularExpressions.Regex.Match(title, standalonePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (standaloneMatch.Success)
        {
            var keyString = standaloneMatch.Groups[1].Value;
            if (TryParseMusicalKey(keyString, out var key))
            {
                return key;
            }
        }

        return null;
    }

    private static MusicalKey? ExtractMusicalKeyFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Look for keys in brackets or standalone in the text
        var patterns = new[]
        {
            @"\(([A-G][#b]?)\)",  // Keys in brackets
            @"\b([A-G][#b]?)\b"   // Standalone keys
        };

        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var keyString = match.Groups[1].Value;
                if (TryParseMusicalKey(keyString, out var key))
                {
                    return key;
                }
            }
        }

        return null;
    }

    private static bool TryParseMusicalKey(string keyString, out MusicalKey key)
    {
        key = MusicalKey.NotSet;

        // Normalize the key string
        var normalizedKey = keyString.Trim().ToUpper();
        
        // Handle common variations
        switch (normalizedKey)
        {
            case "C":
                key = MusicalKey.C;
                return true;
            case "G":
                key = MusicalKey.G;
                return true;
            case "F":
                key = MusicalKey.F;
                return true;
            case "D":
                key = MusicalKey.D;
                return true;
            case "A":
                key = MusicalKey.A;
                return true;
            case "E":
                key = MusicalKey.E;
                return true;
            case "B":
                key = MusicalKey.B;
                return true;
            case "BB":
            case "B♭":
                key = MusicalKey.Bb;
                return true;
            case "AB":
            case "A♭":
                key = MusicalKey.Ab;
                return true;
            case "EB":
            case "E♭":
                key = MusicalKey.Eb;
                return true;
            case "DB":
            case "D♭":
                key = MusicalKey.Db;
                return true;
            case "GB":
            case "G♭":
                key = MusicalKey.Gb;
                return true;
            default:
                return false;
        }
    }
} 