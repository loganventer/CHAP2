using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;
using CHAP2.Common.Models;
using CHAP2.Common.Enum;
using CHAP2.Common.Interfaces;
using CHAP2.Common.Configuration;

namespace CHAP2.Common.Services
{
    // In API, inject values from SlideConversionSettings
    public class SlideToChorusService : ISlideToChorusService
    {
        private readonly ChorusType _defaultChorusType;
        private readonly TimeSignature _defaultTimeSignature;
        private readonly TextStandardizationService _textStandardizationService;

        public SlideToChorusService(ChorusType defaultChorusType, TimeSignature defaultTimeSignature, IConfigurationService configService)
        {
            _defaultChorusType = defaultChorusType;
            _defaultTimeSignature = defaultTimeSignature;
            _textStandardizationService = new TextStandardizationService(configService);
        }

        public Chorus ConvertToChorus(byte[] fileContent, string filename)
        {
            var chorusName = ExtractChorusNameFromFilename(filename);
            var chorusText = ExtractChorusTextFromPowerPoint(fileContent, filename);
            
            // Standardize the title and text
            var standardizedName = _textStandardizationService.StandardizeTitle(chorusName ?? string.Empty);
            var standardizedText = _textStandardizationService.StandardizeChorusText(chorusText);
            
            // Try to extract musical key from title and text
            var musicalKey = ExtractMusicalKeyFromTitle(standardizedName) ?? 
                           ExtractMusicalKeyFromText(standardizedText) ?? 
                           ExtractMusicalKeyFromFilename(filename);

            return new Chorus
            {
                Name = standardizedName,
                Key = musicalKey ?? MusicalKey.NotSet,
                TimeSignature = _defaultTimeSignature,
                ChorusText = standardizedText,
                Type = _defaultChorusType
            };
        }

        private static string? ExtractChorusNameFromFilename(string filename)
        {
            return Path.GetFileNameWithoutExtension(filename);
        }

        private static MusicalKey? ExtractMusicalKeyFromFilename(string filename)
        {
            var filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            foreach (MusicalKey key in System.Enum.GetValues(typeof(MusicalKey)))
            {
                if (key == MusicalKey.NotSet) continue;
                if (filenameWithoutExt.Contains(key.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }
            return null;
        }

        private static MusicalKey? ExtractMusicalKeyFromTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            // Look for keys in brackets (e.g., "Song Title (C)", "Song Title (G)")
            var bracketPattern = @"\(([A-G][#b]?)\)";
            var bracketMatch = Regex.Match(title, bracketPattern, RegexOptions.IgnoreCase);
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
            var standaloneMatch = Regex.Match(title, standalonePattern, RegexOptions.IgnoreCase);
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
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
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

        private static T ParseEnumOrDefault<T>(string value, T defaultValue) where T : struct
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (System.Enum.TryParse<T>(value, true, out var result))
                return result;

            return defaultValue;
        }

        private static string ExtractChorusTextFromPowerPoint(byte[] fileContent, string filename)
        {
            try
            {
                using var stream = new MemoryStream(fileContent);
                using var presentationDocument = PresentationDocument.Open(stream, false);
                
                var presentationPart = presentationDocument.PresentationPart;
                if (presentationPart == null)
                {
                    return $"Could not read PowerPoint file: {filename}";
                }

                var slideParts = presentationPart.SlideParts;
                var extractedText = new StringBuilder();

                foreach (var slidePart in slideParts)
                {
                    var slide = slidePart.Slide;
                    if (slide == null) continue;

                    // Extract text from all text elements in the slide
                    var textElements = slide.Descendants<DrawingText>();
                    foreach (var textElement in textElements)
                    {
                        var text = textElement.Text?.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            extractedText.AppendLine(text);
                        }
                    }
                }

                var result = extractedText.ToString().Trim();
                return string.IsNullOrWhiteSpace(result) 
                    ? $"No text content found in PowerPoint file: {filename}" 
                    : result;
            }
            catch (Exception ex)
            {
                return $"Error extracting text from PowerPoint file: {ex.Message}";
            }
        }
    }
} 