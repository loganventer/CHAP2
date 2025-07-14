using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2API.Controllers;

/// <summary>
/// Health controller for API health monitoring
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ChapControllerAbstractBase
{
    public HealthController(ILogger<HealthController> logger) 
        : base(logger)
    {
    }
    
    /// <summary>
    /// Simple ping endpoint to check if the service is up
    /// </summary>
    /// <returns>200 OK if service is running</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        LogAction("Ping");
        
        return Ok(new 
        { 
            Status = "OK",
            Message = "Service is running"
        });
    }
} 