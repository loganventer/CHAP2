using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;

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
            // Extract chorus name from filename (remove extension)
            var chorusName = Path.GetFileNameWithoutExtension(filename);
            
            // For now, create a basic chorus with the filename as name
            // In a real implementation, you would parse the PowerPoint file content
            // to extract the actual text content
            
            var chorus = Chorus.CreateFromSlide(
                name: chorusName,
                chorusText: $"Converted from {filename}"
            );

            _logger.LogInformation("Successfully converted slide to chorus: {ChorusName}", chorus.Name);
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert slide file: {Filename}", filename);
            throw;
        }
    }
} 