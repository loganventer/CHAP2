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
            if (lines.Length > 0 && string.Equals(lines[0].Trim(), firstLine.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                textWithoutTitle = string.Join("\n", lines.Skip(1)).Trim();
            }

            if (key != MusicalKey.NotSet)
            {
                var chorus = Chorus.Create(
                    name: cleanTitle,
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
                    name: cleanTitle,
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
                        foreach (var run in paragraph.Elements<DocumentFormat.OpenXml.Drawing.Run>())
                        {
                            var text = run.Text?.Text;
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                textBuilder.Append(text);
                            }
                        }
                        textBuilder.AppendLine();
                    }
                }
            }
        }
        
        return textBuilder.ToString().Trim();
    }

    private (string CleanTitle, MusicalKey Key) ExtractKeyFromMultipleSources(string filename, string slideTitle)
    {
        var (slideTitleWithoutKey, slideKey) = ExtractKeyFromText(slideTitle, _keyPatterns);
        
        if (slideKey != MusicalKey.NotSet)
        {
            return (ToTitleCase(slideTitleWithoutKey), slideKey);
        }
        
        var (filenameWithoutKey, filenameKey) = ExtractKeyFromText(filename, _keyPatterns);
        return (ToTitleCase(filenameWithoutKey), filenameKey);
    }

    private (string TextWithoutKey, MusicalKey Key) ExtractKeyFromText(string text, Dictionary<string, MusicalKey> keyPatterns)
    {
        if (string.IsNullOrWhiteSpace(text))
            return (string.Empty, MusicalKey.NotSet);

        var originalText = text.Trim();
        
        var parenMatch = System.Text.RegularExpressions.Regex.Match(originalText, @"\(([^)]+)\)");
        if (parenMatch.Success)
        {
            var keyInParens = parenMatch.Groups[1].Value.Trim();
            if (keyPatterns.TryGetValue(keyInParens, out var key))
            {
                var textWithoutParens = originalText.Replace(parenMatch.Value, "").Trim();
                return (textWithoutParens, key);
            }
        }
        
        var words = originalText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return (originalText, MusicalKey.NotSet);
        
        var lastWord = words[^1];
        if (keyPatterns.TryGetValue(lastWord, out var singleKey))
        {
            var textWithoutKey = string.Join(" ", words.Take(words.Length - 1));
            return (textWithoutKey, singleKey);
        }
        
        if (words.Length > 1)
        {
            var lastTwoWords = $"{words[^2]} {words[^1]}";
            if (keyPatterns.TryGetValue(lastTwoWords, out var twoWordKey))
            {
                var textWithoutKey = string.Join(" ", words.Take(words.Length - 2));
                return (textWithoutKey, twoWordKey);
            }
        }
        
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
        return new Dictionary<string, MusicalKey>(StringComparer.OrdinalIgnoreCase)
        {
            { "C", MusicalKey.C }, { "D", MusicalKey.D }, { "E", MusicalKey.E }, { "F", MusicalKey.F }, 
            { "G", MusicalKey.G }, { "A", MusicalKey.A }, { "B", MusicalKey.B },
            
            { "C#", MusicalKey.CSharp }, { "C♯", MusicalKey.CSharp }, { "C-sharp", MusicalKey.CSharp }, { "Cis", MusicalKey.CSharp },
            { "D#", MusicalKey.DSharp }, { "D♯", MusicalKey.DSharp }, { "D-sharp", MusicalKey.DSharp }, { "Dis", MusicalKey.DSharp },
            { "F#", MusicalKey.FSharp }, { "F♯", MusicalKey.FSharp }, { "F-sharp", MusicalKey.FSharp }, { "Fis", MusicalKey.FSharp },
            { "G#", MusicalKey.GSharp }, { "G♯", MusicalKey.GSharp }, { "G-sharp", MusicalKey.GSharp }, { "Gis", MusicalKey.GSharp },
            { "A#", MusicalKey.ASharp }, { "A♯", MusicalKey.ASharp }, { "A-sharp", MusicalKey.ASharp }, { "Ais", MusicalKey.ASharp },
            
            { "Cb", MusicalKey.CFlat }, { "C-flat", MusicalKey.CFlat },
            { "Db", MusicalKey.DFlat }, { "D♭", MusicalKey.DFlat }, { "D-flat", MusicalKey.DFlat }, { "Des", MusicalKey.DFlat },
            { "Eb", MusicalKey.EFlat }, { "E♭", MusicalKey.EFlat }, { "E-flat", MusicalKey.EFlat }, { "E-mol", MusicalKey.EFlat }, { "Es", MusicalKey.EFlat },
            { "Gb", MusicalKey.GFlat }, { "G♭", MusicalKey.GFlat }, { "G-flat", MusicalKey.GFlat }, { "Ges", MusicalKey.GFlat },
            { "Ab", MusicalKey.AFlat }, { "A♭", MusicalKey.AFlat }, { "A-flat", MusicalKey.AFlat }, { "As", MusicalKey.AFlat },
            { "Bb", MusicalKey.BFlat }, { "B♭", MusicalKey.BFlat }, { "B-flat", MusicalKey.BFlat }, { "B-mol", MusicalKey.BFlat }, { "Bes", MusicalKey.BFlat },
        };
    }
} 