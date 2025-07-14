using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;

namespace CHAP2API.Controllers;

[ApiController]
[Route("[controller]")]
public class SlideController : ChapControllerAbstractBase
{
    private readonly IChorusResource _chorusResource;
    private readonly ISlideToChorusService _slideToChorusService;
    
    public SlideController(ILogger<SlideController> logger, IChorusResource chorusResource, ISlideToChorusService slideToChorusService)
        : base(logger)
    {
        _chorusResource = chorusResource;
        _slideToChorusService = slideToChorusService;
    }

    /// <summary>
    /// Convert PowerPoint slide file (raw .ppsx binary) to chorus structure
    /// </summary>
    [HttpPost("convert")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ConvertSlideToChorus([FromHeader(Name = "X-Filename")] string filename)
    {
        _logger.LogInformation("=== SLIDE CONVERT ENDPOINT HIT ===");
        LogAction("ConvertSlideToChorus", new { filename = filename });
        _logger.LogInformation("Request Content-Type: {ContentType}", Request.ContentType);
        _logger.LogInformation("Request Headers: {Headers}", string.Join(", ", Request.Headers.Keys));

        // Read the request body directly
        byte[] fileContent;
        using (var memoryStream = new MemoryStream())
        {
            await Request.Body.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        if (fileContent == null || fileContent.Length == 0)
        {
            _logger.LogWarning("No file data provided");
            return BadRequest("No file data provided or file is empty");
        }
        if (string.IsNullOrWhiteSpace(filename))
        {
            _logger.LogWarning("No filename header provided");
            return BadRequest("Filename header (X-Filename) is required");
        }

        // Validate file extension
        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (extension != ".ppsx")
        {
            return BadRequest("Only .ppsx files are allowed");
        }

        // Use the service to convert to Chorus
        var chorus = _slideToChorusService.ConvertToChorus(fileContent, filename);

        if (string.IsNullOrWhiteSpace(chorus.Name))
        {
            return BadRequest("Could not extract chorus name from slide file");
        }

        // Check if a chorus with the same name already exists
        var existingChorus = await _chorusResource.GetChorusByNameAsync(chorus.Name);
        
        if (existingChorus != null)
        {
            // If the text content is different, update the existing chorus
            if (!string.Equals(existingChorus.ChorusText, chorus.ChorusText, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Updating existing chorus '{Name}' with new text content", chorus.Name);
                
                // Update the existing chorus with new text content
                existingChorus.ChorusText = chorus.ChorusText;
                await _chorusResource.UpdateChorusAsync(existingChorus);
                
                return Ok(new 
                { 
                    message = $"Updated existing chorus '{chorus.Name}' with new text content",
                    chorus = existingChorus,
                    originalFilename = filename,
                    action = "updated"
                });
            }
            else
            {
                // Text content is the same, no update needed
                return Ok(new 
                { 
                    message = $"Chorus '{chorus.Name}' already exists with identical text content",
                    chorus = existingChorus,
                    originalFilename = filename,
                    action = "no_change"
                });
            }
        }

        // No existing chorus found, create a new one
        await _chorusResource.AddChorusAsync(chorus);
        
        return Created($"/api/choruses/{chorus.Id}", new 
        { 
            message = $"Successfully converted PowerPoint file to chorus: {chorus.Name}",
            chorus = chorus,
            originalFilename = filename,
            action = "created"
        });
    }
} 