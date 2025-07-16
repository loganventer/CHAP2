using Microsoft.AspNetCore.Mvc;
using CHAP2.Application.Interfaces;
using CHAP2.Chorus.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SlideController : ChapControllerAbstractBase
{
    private readonly IChorusRepository _chorusResource;
    private readonly ISlideToChorusService _slideToChorusService;
    private readonly SlideConversionSettings _slideSettings;
    
    public SlideController(
        ILogger<SlideController> logger, 
        IChorusRepository chorusResource, 
        ISlideToChorusService slideToChorusService,
        IOptions<SlideConversionSettings> slideSettings)
        : base(logger)
    {
        _chorusResource = chorusResource;
        _slideToChorusService = slideToChorusService;
        _slideSettings = slideSettings.Value;
    }

    [HttpPost("convert")]
    [Consumes("application/octet-stream")]
    public async Task<IActionResult> ConvertSlideToChorus([FromHeader(Name = "X-Filename")] string filename)
    {
        LogAction("ConvertSlideToChorus", new { filename });

        var fileContent = await ReadRequestBodyAsync();
        if (!ValidateFileContent(fileContent, filename))
        {
            return BadRequest("Invalid file content or filename");
        }

        var chorus = _slideToChorusService.ConvertToChorus(fileContent, filename);
        if (string.IsNullOrWhiteSpace(chorus.Name))
        {
            return BadRequest("Could not extract chorus name from slide file");
        }

        var existingChorus = await _chorusResource.GetByNameAsync(chorus.Name);
        if (existingChorus != null)
        {
            return await HandleExistingChorus(existingChorus, chorus, filename);
        }

        await _chorusResource.AddAsync(chorus);
        
        return Created($"/api/choruses/{chorus.Id}", new 
        { 
            message = $"Successfully converted PowerPoint file to chorus: {chorus.Name}",
            chorus = chorus,
            originalFilename = filename,
            action = "created"
        });
    }

    private async Task<byte[]> ReadRequestBodyAsync()
    {
        using var memoryStream = new MemoryStream();
        await Request.Body.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private bool ValidateFileContent(byte[] fileContent, string filename)
    {
        if (fileContent == null || fileContent.Length == 0)
        {
            _logger.LogWarning("No file data provided");
            return false;
        }
        
        if (fileContent.Length > _slideSettings.MaxFileSizeBytes)
        {
            _logger.LogWarning("File size exceeds maximum allowed size: {FileSize} > {MaxSize}", 
                fileContent.Length, _slideSettings.MaxFileSizeBytes);
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(filename))
        {
            _logger.LogWarning("No filename header provided");
            return false;
        }

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        if (!_slideSettings.AllowedExtensions.Contains(extension))
        {
            _logger.LogWarning("Invalid file extension: {Extension}", extension);
            return false;
        }

        return true;
    }

    private async Task<IActionResult> HandleExistingChorus(CHAP2.Domain.Entities.Chorus existingChorus, CHAP2.Domain.Entities.Chorus newChorus, string filename)
    {
        if (!string.Equals(existingChorus.ChorusText, newChorus.ChorusText, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Updating existing chorus '{Name}' with new text content", newChorus.Name);
            
            existingChorus.Update(
                existingChorus.Name,
                newChorus.ChorusText,
                existingChorus.Key,
                existingChorus.Type,
                existingChorus.TimeSignature
            );
            await _chorusResource.UpdateAsync(existingChorus);
            
            return Ok(new 
            { 
                message = $"Updated existing chorus '{newChorus.Name}' with new text content",
                chorus = existingChorus,
                originalFilename = filename,
                action = "updated"
            });
        }

        return Ok(new 
        { 
            message = $"Chorus '{newChorus.Name}' already exists with identical text content",
            chorus = existingChorus,
            originalFilename = filename,
            action = "no_change"
        });
    }
} 