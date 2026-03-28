using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CHAP2.Chorus.Api.Controllers;

public abstract class ChapControllerAbstractBase : ControllerBase
{
    protected readonly ILogger _logger;
    
    protected ChapControllerAbstractBase(ILogger logger)
    {
        _logger = logger;
    }
    
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