using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;

namespace CHAP2API.Controllers;

/// <summary>
/// Choruses controller
/// </summary>
[ApiController]
[Route("[controller]")]
public class ChorusesController : ChapControllerAbstractBase
{
    private readonly IChorusResource _chorusResource;
    public ChorusesController(ILogger<ChorusesController> logger, IChorusResource chorusResource) 
        : base(logger)
    {
        _chorusResource = chorusResource;
    }

    /// <summary>
    /// Add a new chorus
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddChorus([FromBody] Chorus chorus)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        await _chorusResource.AddChorusAsync(chorus);
        return Created("", chorus);
    }

    /// <summary>
    /// Get all choruses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllChoruses()
    {
        var choruses = await _chorusResource.GetAllChorusesAsync();
        return Ok(choruses);
    }
} 