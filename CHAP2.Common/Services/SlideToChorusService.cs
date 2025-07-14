using System;
using System.IO;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DrawingText = DocumentFormat.OpenXml.Drawing.Text;
using CHAP2.Common.Models;
using CHAP2.Common.Enum;
using CHAP2.Common.Interfaces;

namespace CHAP2.Common.Services
{
    public class SlideToChorusService : ISlideToChorusService
    {
        public Chorus ConvertToChorus(byte[] fileContent, string filename)
        {
            var chorusName = ExtractChorusNameFromFilename(filename);
            var musicalKey = ExtractMusicalKeyFromFilename(filename);
            var chorusText = ExtractChorusTextFromPowerPoint(fileContent, filename);

            return new Chorus
            {
                Name = chorusName,
                Key = musicalKey ?? MusicalKey.NotSet,
                TimeSignature = TimeSignature.NotSet,
                ChorusText = chorusText,
                Type = ChorusType.NotSet
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