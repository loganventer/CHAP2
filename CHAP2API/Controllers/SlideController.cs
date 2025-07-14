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
    public async Task<IActionResult> ConvertSlideToChorus([FromBody] byte[] fileContent, [FromHeader(Name = "X-Filename")] string filename)
    {
        LogAction("ConvertSlideToChorus", new { filename = filename, size = fileContent?.Length });
        
        if (fileContent == null || fileContent.Length == 0)
        {
            return BadRequest("No file data provided or file is empty");
        }
        if (string.IsNullOrWhiteSpace(filename))
        {
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
        if (await _chorusResource.ChorusExistsAsync(chorus.Name))
        {
            return Conflict($"A chorus with the name '{chorus.Name}' already exists.");
        }

        await _chorusResource.AddChorusAsync(chorus);
        
        return Created($"/api/choruses/{chorus.Id}", new 
        { 
            message = $"Successfully converted PowerPoint file to chorus: {chorus.Name}",
            chorus = chorus,
            originalFilename = filename
        });
    }
} 