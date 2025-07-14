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
        LogAction("AddChorus", new { chorus.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if a chorus with the same name already exists
        if (await _chorusResource.ChorusExistsAsync(chorus.Name))
        {
            return Conflict($"A chorus with the name '{chorus.Name}' already exists.");
        }

        await _chorusResource.AddChorusAsync(chorus);
        return CreatedAtAction(nameof(GetChorusById), new { id = chorus.Id }, chorus);
    }

    /// <summary>
    /// Get all choruses
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllChoruses()
    {
        LogAction("GetAllChoruses");
        
        var choruses = await _chorusResource.GetAllChorusesAsync();
        return Ok(choruses);
    }

    /// <summary>
    /// Get a chorus by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetChorusById(Guid id)
    {
        LogAction("GetChorusById", new { id });
        
        var chorus = await _chorusResource.GetChorusByIdAsync(id);
        if (chorus == null)
            return NotFound();
        return Ok(chorus);
    }

    /// <summary>
    /// Update a chorus
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateChorus(Guid id, [FromBody] Chorus chorus)
    {
        LogAction("UpdateChorus", new { id, chorus.Name });
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        if (id != chorus.Id)
            return BadRequest("ID mismatch");
            
        var existingChorus = await _chorusResource.GetChorusByIdAsync(id);
        if (existingChorus == null)
            return NotFound();

        // Check if the new name conflicts with another chorus (excluding this one)
        var choruses = await _chorusResource.GetAllChorusesAsync();
        var nameConflict = choruses.Any(c => 
            c.Id != id && 
            string.Equals(c.Name, chorus.Name, StringComparison.OrdinalIgnoreCase));
            
        if (nameConflict)
        {
            return Conflict($"A chorus with the name '{chorus.Name}' already exists.");
        }
            
        await _chorusResource.UpdateChorusAsync(chorus);
        return Ok(chorus);
    }
} 