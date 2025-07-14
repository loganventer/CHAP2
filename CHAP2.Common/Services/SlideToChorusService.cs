using System;
using System.IO;
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
            // Placeholder logic
            var chorusName = ExtractChorusNameFromFilename(filename);
            return $"Chorus text extracted from PowerPoint file: {chorusName}";
        }
    }
} 