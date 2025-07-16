using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using System.Text;
using System.Linq;

namespace CHAP2.Application.Services;

public class SlideToChorusService : ISlideToChorusService
{
    private readonly ILogger<SlideToChorusService> _logger;
    private readonly Dictionary<string, MusicalKey> _keyPatterns;

    public SlideToChorusService(ILogger<SlideToChorusService> logger)
    {
        _logger = logger;
        _keyPatterns = InitializeKeyPatterns();
    }

    public Chorus ConvertToChorus(byte[] fileContent, string filename)
    {
        _logger.LogInformation("Converting slide file: {Filename}", filename);

        try
        {
            var chorusName = System.IO.Path.GetFileNameWithoutExtension(filename);
            var chorusText = ExtractTextFromPowerPoint(fileContent);

            if (string.IsNullOrWhiteSpace(chorusText))
            {
                var msg = $"No text content could be extracted from PowerPoint file: {filename}";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            var lines = chorusText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var firstLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? chorusName;
            
            var (cleanTitle, key) = ExtractKeyFromMultipleSources(chorusName, firstLine);

            string textWithoutTitle = chorusText;
            // Only remove the first line if it's a duplicate of the title AND there are multiple lines
            // This prevents removing the first line when it's actually part of the chorus content
            if (lines.Length > 1 && string.Equals(lines[0].Trim(), cleanTitle.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                textWithoutTitle = string.Join("\n", lines.Skip(1)).Trim();
                _logger.LogInformation("Removed duplicate title line from chorus text for: {ChorusName}", cleanTitle);
            }
            else
            {
                _logger.LogInformation("Keeping all lines in chorus text for: {ChorusName} (first line doesn't match title or only one line)", cleanTitle);
            }

            var chorus = Chorus.CreateFromSlide(
                name: cleanTitle,
                chorusText: textWithoutTitle
            );
            
            // Set the key if found
            if (key != MusicalKey.NotSet)
            {
                var chorusType = typeof(Chorus);
                var keyProperty = chorusType.GetProperty("Key");
                if (keyProperty != null)
                {
                    keyProperty.SetValue(chorus, key);
                    _logger.LogInformation("Successfully converted slide to chorus: {ChorusName} with key {Key} and {TextLength} characters", 
                        chorus.Name, key, textWithoutTitle.Length);
                }
            }
            else
            {
                _logger.LogInformation("Successfully converted slide to chorus: {ChorusName} (no key detected) with {TextLength} characters", 
                    chorus.Name, textWithoutTitle.Length);
            }
            
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert slide file: {Filename}", filename);
            throw;
        }
    }

    private string ExtractTextFromPowerPoint(byte[] fileContent)
    {
        try
        {
            _logger.LogInformation("Starting PowerPoint text extraction from {FileSize} bytes", fileContent.Length);
            
            using var stream = new MemoryStream(fileContent);
            using var presentationDocument = PresentationDocument.Open(stream, false);
            
            var presentationPart = presentationDocument.PresentationPart;
            if (presentationPart == null)
            {
                _logger.LogWarning("No presentation part found in PowerPoint file");
                return string.Empty;
            }

            var slideIds = presentationPart.Presentation?.SlideIdList?.Elements<SlideId>();
            if (slideIds == null || !slideIds.Any())
            {
                _logger.LogWarning("No slides found in PowerPoint file");
                return string.Empty;
            }

            _logger.LogInformation("Found {SlideCount} slides", slideIds.Count());

            var allText = new StringBuilder();
            
            foreach (var slideId in slideIds)
            {
                var slidePart = presentationPart.GetPartById(slideId.RelationshipId!) as SlidePart;
                if (slidePart?.Slide?.CommonSlideData?.ShapeTree == null)
                {
                    _logger.LogWarning("Slide part or shape tree is null for slide ID: {SlideId}", slideId.Id);
                    continue;
                }

                var slideText = ExtractTextFromShapeTree(slidePart.Slide.CommonSlideData.ShapeTree);
                if (!string.IsNullOrWhiteSpace(slideText))
                {
                    _logger.LogInformation("Extracted {TextLength} characters from slide", slideText.Length);
                    allText.AppendLine(slideText);
                }
                else
                {
                    _logger.LogWarning("No text extracted from slide ID: {SlideId}", slideId.Id);
                }
            }

            var finalText = allText.ToString().Trim();
            _logger.LogInformation("Total extracted text length: {TextLength} characters", finalText.Length);
            return finalText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PowerPoint file");
            return string.Empty;
        }
    }

    private string ExtractTextFromShapeTree(OpenXmlCompositeElement shapeTree)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var element in shapeTree.Elements())
        {
            if (element is DocumentFormat.OpenXml.Presentation.Shape shapeElement)
            {
                var textBody = shapeElement.TextBody;
                if (textBody != null)
                {
                    foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
                    {
                        foreach (var childElement in paragraph.Elements())
                        {
                            if (childElement is DocumentFormat.OpenXml.Drawing.Run run)
                            {
                                var text = run.Text?.Text;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    textBuilder.Append(text);
                                }
                            }
                            else if (childElement is DocumentFormat.OpenXml.Drawing.Break)
                            {
                                // Handle line breaks within paragraphs
                                textBuilder.AppendLine();
                            }
                            else if (childElement is DocumentFormat.OpenXml.Drawing.Text textElement)
                            {
                                // Handle direct text elements
                                var text = textElement.Text;
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    textBuilder.Append(text);
                                }
                            }
                        }
                        
                        // Add newline after each paragraph
                        textBuilder.AppendLine();
                    }
                }
            }
        }
        
        return textBuilder.ToString().Trim();
    }

    private (string CleanTitle, MusicalKey Key) ExtractKeyFromMultipleSources(string filename, string slideTitle)
    {
        _logger.LogInformation("Extracting key from filename: '{Filename}' and slide title: '{SlideTitle}'", filename, slideTitle);
        
        var (slideTitleWithoutKey, slideKey) = ExtractKeyFromText(slideTitle, _keyPatterns);
        _logger.LogInformation("From slide title - Key: {Key}, Title without key: '{TitleWithoutKey}'", slideKey, slideTitleWithoutKey);
        
        if (slideKey != MusicalKey.NotSet)
        {
            _logger.LogInformation("Found key in slide title: {Key}", slideKey);
            return (ToTitleCase(slideTitleWithoutKey), slideKey);
        }
        
        var (filenameWithoutKey, filenameKey) = ExtractKeyFromText(filename, _keyPatterns);
        _logger.LogInformation("From filename - Key: {Key}, Filename without key: '{FilenameWithoutKey}'", filenameKey, filenameWithoutKey);
        
        if (filenameKey != MusicalKey.NotSet)
        {
            _logger.LogInformation("Found key in filename: {Key}", filenameKey);
        }
        
        return (ToTitleCase(filenameWithoutKey), filenameKey);
    }

    private (string TextWithoutKey, MusicalKey Key) ExtractKeyFromText(string text, Dictionary<string, MusicalKey> keyPatterns)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Text is null or whitespace, returning NotSet");
            return (string.Empty, MusicalKey.NotSet);
        }

        var originalText = text.Trim();
        _logger.LogDebug("Processing text: '{OriginalText}'", originalText);
        
        // First, try to find key in parentheses
        var parenMatch = System.Text.RegularExpressions.Regex.Match(originalText, @"\(([^)]+)\)");
        if (parenMatch.Success)
        {
            var keyInParens = parenMatch.Groups[1].Value.Trim();
            _logger.LogDebug("Found parentheses with content: '{KeyInParens}'", keyInParens);
            
            if (keyPatterns.TryGetValue(keyInParens, out var key))
            {
                var textWithoutParens = originalText.Replace(parenMatch.Value, "").Trim();
                _logger.LogDebug("Matched key in parentheses: {Key}, text without parens: '{TextWithoutParens}'", key, textWithoutParens);
                return (textWithoutParens, key);
            }
            else
            {
                _logger.LogDebug("No key pattern match found for: '{KeyInParens}'", keyInParens);
            }
        }
        
        // Try to find key at the end of the text (common in filenames)
        var words = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) 
        {
            _logger.LogDebug("No words found in text");
            return (originalText, MusicalKey.NotSet);
        }
        
        // Check last word for single character key
        var lastWord = words[^1];
        _logger.LogDebug("Checking last word: '{LastWord}'", lastWord);
        
        if (keyPatterns.TryGetValue(lastWord, out var singleKey))
        {
            var textWithoutKey = string.Join(" ", words.Take(words.Length - 1));
            _logger.LogDebug("Matched single word key: {Key}, text without key: '{TextWithoutKey}'", singleKey, textWithoutKey);
            return (textWithoutKey, singleKey);
        }
        
        // Check for two-word keys (like "C sharp", "B flat")
        if (words.Length > 1)
        {
            var lastTwoWords = $"{words[^2]} {words[^1]}";
            _logger.LogDebug("Checking last two words: '{LastTwoWords}'", lastTwoWords);
            
            if (keyPatterns.TryGetValue(lastTwoWords, out var twoWordKey))
            {
                var textWithoutKey = string.Join(" ", words.Take(words.Length - 2));
                _logger.LogDebug("Matched two-word key: {Key}, text without key: '{TextWithoutKey}'", twoWordKey, textWithoutKey);
                return (textWithoutKey, twoWordKey);
            }
        }
        
        // Try to find single letter keys anywhere in the text (for cases like "Amazing Grace C")
        foreach (var word in words)
        {
            if (keyPatterns.TryGetValue(word, out var foundKey))
            {
                var textWithoutKey = string.Join(" ", words.Where(w => w != word));
                _logger.LogDebug("Matched key in word '{Word}': {Key}, text without key: '{TextWithoutKey}'", word, foundKey, textWithoutKey);
                return (textWithoutKey, foundKey);
            }
        }
        
        _logger.LogDebug("No key pattern matches found for text: '{OriginalText}'", originalText);
        return (originalText, MusicalKey.NotSet);
    }

    private string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }

    private Dictionary<string, MusicalKey> InitializeKeyPatterns()
    {
        var patterns = new Dictionary<string, MusicalKey>(StringComparer.OrdinalIgnoreCase)
        {
            // Natural keys
            { "C", MusicalKey.C }, { "D", MusicalKey.D }, { "E", MusicalKey.E }, { "F", MusicalKey.F }, 
            { "G", MusicalKey.G }, { "A", MusicalKey.A }, { "B", MusicalKey.B },
            
            // Sharp keys
            { "C#", MusicalKey.CSharp }, { "C♯", MusicalKey.CSharp }, { "C-sharp", MusicalKey.CSharp }, { "Cis", MusicalKey.CSharp },
            { "D#", MusicalKey.DSharp }, { "D♯", MusicalKey.DSharp }, { "D-sharp", MusicalKey.DSharp }, { "Dis", MusicalKey.DSharp },
            { "F#", MusicalKey.FSharp }, { "F♯", MusicalKey.FSharp }, { "F-sharp", MusicalKey.FSharp }, { "Fis", MusicalKey.FSharp },
            { "G#", MusicalKey.GSharp }, { "G♯", MusicalKey.GSharp }, { "G-sharp", MusicalKey.GSharp }, { "Gis", MusicalKey.GSharp },
            { "A#", MusicalKey.ASharp }, { "A♯", MusicalKey.ASharp }, { "A-sharp", MusicalKey.ASharp }, { "Ais", MusicalKey.ASharp },
            
            // Flat keys
            { "Cb", MusicalKey.CFlat }, { "C-flat", MusicalKey.CFlat },
            { "Db", MusicalKey.DFlat }, { "D♭", MusicalKey.DFlat }, { "D-flat", MusicalKey.DFlat }, { "Des", MusicalKey.DFlat },
            { "Eb", MusicalKey.EFlat }, { "E♭", MusicalKey.EFlat }, { "E-flat", MusicalKey.EFlat }, { "E-mol", MusicalKey.EFlat }, { "Es", MusicalKey.EFlat },
            { "Gb", MusicalKey.GFlat }, { "G♭", MusicalKey.GFlat }, { "G-flat", MusicalKey.GFlat }, { "Ges", MusicalKey.GFlat },
            { "Ab", MusicalKey.AFlat }, { "A♭", MusicalKey.AFlat }, { "A-flat", MusicalKey.AFlat }, { "As", MusicalKey.AFlat },
            { "Bb", MusicalKey.BFlat }, { "B♭", MusicalKey.BFlat }, { "B-flat", MusicalKey.BFlat }, { "B-mol", MusicalKey.BFlat }, { "Bes", MusicalKey.BFlat },
            
            // Additional common notations
            { "C major", MusicalKey.C }, { "D major", MusicalKey.D }, { "E major", MusicalKey.E }, { "F major", MusicalKey.F },
            { "G major", MusicalKey.G }, { "A major", MusicalKey.A }, { "B major", MusicalKey.B },
            { "C minor", MusicalKey.C }, { "D minor", MusicalKey.D }, { "E minor", MusicalKey.E }, { "F minor", MusicalKey.F },
            { "G minor", MusicalKey.G }, { "A minor", MusicalKey.A }, { "B minor", MusicalKey.B },
            
            // Key in key format (e.g., "Key of C", "Key: C")
            { "Key of C", MusicalKey.C }, { "Key of D", MusicalKey.D }, { "Key of E", MusicalKey.E }, { "Key of F", MusicalKey.F },
            { "Key of G", MusicalKey.G }, { "Key of A", MusicalKey.A }, { "Key of B", MusicalKey.B },
            { "Key: C", MusicalKey.C }, { "Key: D", MusicalKey.D }, { "Key: E", MusicalKey.E }, { "Key: F", MusicalKey.F },
            { "Key: G", MusicalKey.G }, { "Key: A", MusicalKey.A }, { "Key: B", MusicalKey.B },
        };
        
        _logger.LogInformation("Initialized {Count} key patterns", patterns.Count);
        foreach (var pattern in patterns.Take(10)) // Log first 10 for debugging
        {
            _logger.LogDebug("Key pattern: '{Pattern}' -> {Key}", pattern.Key, pattern.Value);
        }
        
        return patterns;
    }
} 