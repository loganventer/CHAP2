using CHAP2API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CHAP2API.Controllers;

/// <summary>
/// Abstract base controller for all CHAP controllers
/// </summary>
public abstract class ChapControllerAbstractBase : ControllerBase, IController
{
    protected readonly ILogger _logger;
    
    protected ChapControllerAbstractBase(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Common method to log controller actions
    /// </summary>
    /// <param name="action">The action being performed</param>
    /// <param name="parameters">Optional parameters</param>
    protected void LogAction(string action, object? parameters = null)
    {
        _logger.LogInformation("Controller {ControllerType} performing action: {Action}", 
            GetType().Name, action);
        
        if (parameters != null)
        {
            _logger.LogDebug("Action parameters: {@Parameters}", parameters);
        }
    }
} 