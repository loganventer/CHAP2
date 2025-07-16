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

/// <summary>
/// Service for converting PowerPoint slides to chorus entities
/// </summary>
public class SlideToChorusService : ISlideToChorusService
{
    private readonly ILogger<SlideToChorusService> _logger;

    public SlideToChorusService(ILogger<SlideToChorusService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert PowerPoint slide file content to a chorus entity
    /// </summary>
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

            // Use the first non-empty line as the title if possible
            var lines = chorusText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var firstLine = lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? chorusName;
            
            // Try to extract key from both filename and slide title
            var (normalizedTitle, key) = ExtractKeyFromMultipleSources(chorusName, firstLine);

            // Remove the title line from the chorus text if it matches
            string textWithoutTitle = chorusText;
            if (lines.Length > 0 && string.Equals(lines[0].Trim(), firstLine.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                textWithoutTitle = string.Join("\n", lines.Skip(1)).Trim();
            }

            if (key != CHAP2.Domain.Enums.MusicalKey.NotSet)
            {
                var chorus = Chorus.Create(
                    name: normalizedTitle,
                    chorusText: textWithoutTitle,
                    key: key,
                    type: ChorusType.NotSet,
                    timeSignature: TimeSignature.NotSet
                );
                _logger.LogInformation("Successfully converted slide to chorus: {ChorusName} with key {Key} and {TextLength} characters", 
                    chorus.Name, key, textWithoutTitle.Length);
                return chorus;
            }
            else
            {
                var chorus = Chorus.CreateFromSlide(
                    name: normalizedTitle,
                    chorusText: textWithoutTitle
                );
                _logger.LogInformation("Successfully converted slide to chorus: {ChorusName} (no key detected) with {TextLength} characters", 
                    chorus.Name, textWithoutTitle.Length);
                return chorus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert slide file: {Filename}", filename);
            throw;
        }
    }

    /// <summary>
    /// Extract text content from PowerPoint file bytes
    /// </summary>
    private string ExtractTextFromPowerPoint(byte[] fileContent)
    {
        try
        {
            _logger.LogInformation("Starting PowerPoint text extraction from {FileSize} bytes", fileContent.Length);
            
            using var stream = new MemoryStream(fileContent);
            using var presentationDocument = PresentationDocument.Open(stream, false);
            
            _logger.LogInformation("Successfully opened PowerPoint document");
            
            var presentationPart = presentationDocument.PresentationPart;
            if (presentationPart == null)
            {
                _logger.LogWarning("No presentation part found in PowerPoint file");
                return string.Empty;
            }

            _logger.LogInformation("Found presentation part");

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
                _logger.LogInformation("Processing slide with ID: {SlideId}", slideId.Id);
                
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

    /// <summary>
    /// Extract text from a shape tree (slide content)
    /// </summary>
    private string ExtractTextFromShapeTree(OpenXmlCompositeElement shapeTree)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var element in shapeTree.Elements())
        {
            _logger.LogDebug("Processing shape type: {ShapeType}", element.GetType().Name);
            
            if (element is DocumentFormat.OpenXml.Presentation.Shape shapeElement)
            {
                var textBody = shapeElement.TextBody;
                if (textBody != null)
                {
                    _logger.LogDebug("Found text body in shape");
                    
                    foreach (var paragraph in textBody.Elements<DocumentFormat.OpenXml.Drawing.Paragraph>())
                    {
                        foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
                        {
                            var text = run.Text?.Text;
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                _logger.LogDebug("Extracted text: {Text}", text);
                                textBuilder.Append(text);
                            }
                        }
                        textBuilder.AppendLine(); // Add line break between paragraphs
                    }
                }
                else
                {
                    _logger.LogDebug("No text body found in shape");
                }
            }
            else if (element is DocumentFormat.OpenXml.Drawing.Table table)
            {
                foreach (var row in table.Elements<DocumentFormat.OpenXml.Drawing.TableRow>())
                {
                    foreach (var cell in row.Elements<DocumentFormat.OpenXml.Drawing.TableCell>())
                    {
                        var cellText = ExtractTextFromShapeTree(cell);
                        if (!string.IsNullOrWhiteSpace(cellText))
                        {
                            textBuilder.Append(cellText + " ");
                        }
                    }
                    textBuilder.AppendLine();
                }
            }
            else if (element is DocumentFormat.OpenXml.Presentation.GroupShape groupShape)
            {
                var groupText = ExtractTextFromShapeTree(groupShape);
                if (!string.IsNullOrWhiteSpace(groupText))
                {
                    textBuilder.AppendLine(groupText);
                }
            }
            else
            {
                _logger.LogDebug("Skipping shape type: {ShapeType}", element.GetType().Name);
            }
        }
        
        var result = textBuilder.ToString().Trim();
        _logger.LogDebug("Extracted text from shape tree: {Text}", result);
        return result;
    }

    /// <summary>
    /// Extracts the musical key from multiple sources (filename and slide title) and normalizes the title to title case.
    /// </summary>
    private (string NormalizedTitle, MusicalKey Key) ExtractKeyFromMultipleSources(string filename, string slideTitle)
    {
        // Comprehensive key patterns including various notations
        var keyPatterns = new Dictionary<string, MusicalKey>(StringComparer.OrdinalIgnoreCase)
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
        };

        // Try to extract key from filename first
        var (filenameTitle, filenameKey) = ExtractKeyFromText(filename, keyPatterns);
        
        // Try to extract key from slide title
        var (slideTitleWithoutKey, slideKey) = ExtractKeyFromText(slideTitle, keyPatterns);
        
        // Prefer the key from slide title if both have keys, otherwise use whichever has a key
        var finalKey = slideKey != MusicalKey.NotSet ? slideKey : filenameKey;
        var finalTitle = slideKey != MusicalKey.NotSet ? slideTitleWithoutKey : filenameTitle;
        
        // If no key found in either, use the slide title as the final title
        if (finalKey == MusicalKey.NotSet)
        {
            finalTitle = ToTitleCase(slideTitle);
        }
        
        return (finalTitle, finalKey);
    }

    /// <summary>
    /// Extracts the musical key from text and returns the text without the key.
    /// </summary>
    private (string TextWithoutKey, MusicalKey Key) ExtractKeyFromText(string text, Dictionary<string, MusicalKey> keyPatterns)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, MusicalKey.NotSet);

        var originalText = text.Trim();
        
        // Try to find key in parentheses first (e.g., "Hy is Heer G (C)" or "Hy is Heer G (B-mol)")
        var parenMatch = System.Text.RegularExpressions.Regex.Match(originalText, @"\(([^)]+)\)");
        if (parenMatch.Success)
        {
            var keyInParens = parenMatch.Groups[1].Value.Trim();
            if (keyPatterns.TryGetValue(keyInParens, out var key))
            {
                var textWithoutParens = originalText.Replace(parenMatch.Value, "").Trim();
                return (ToTitleCase(textWithoutParens), key);
            }
        }
        
        // Try to find key at the end of the text
        var words = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return (ToTitleCase(originalText), MusicalKey.NotSet);
        
        // Check last word for single character key
        var lastWord = words[^1];
        if (keyPatterns.TryGetValue(lastWord, out var singleKey))
        {
            var textWithoutKey = string.Join(" ", words.Take(words.Length - 1));
            return (ToTitleCase(textWithoutKey), singleKey);
        }
        
        // Check for two-word keys (e.g., "B-mol", "C-sharp")
        if (words.Length > 1)
        {
            var lastTwoWords = $"{words[^2]} {words[^1]}";
            if (keyPatterns.TryGetValue(lastTwoWords, out var twoWordKey))
            {
                var textWithoutKey = string.Join(" ", words.Take(words.Length - 2));
                return (ToTitleCase(textWithoutKey), twoWordKey);
            }
        }
        
        // No key found
        return (ToTitleCase(originalText), MusicalKey.NotSet);
    }

    /// <summary>
    /// Converts a string to title case (every word capitalized, rest lower case)
    /// </summary>
    private string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }
} 