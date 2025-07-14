using CHAP2API.Interfaces;

namespace CHAP2API.Services;

/// <summary>
/// Implementation of IServices interface
/// </summary>
public class Services : IServices
{
    private readonly ILogger<Services> _logger;
    
    public Services(ILogger<Services> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Gets the current application name
    /// </summary>
    /// <returns>The application name</returns>
    public string GetApplicationName()
    {
        _logger.LogInformation("Getting application name");
        return "CHAP2API";
    }
    
    /// <summary>
    /// Gets the current timestamp
    /// </summary>
    /// <returns>The current timestamp</returns>
    public DateTime GetCurrentTimestamp()
    {
        _logger.LogInformation("Getting current timestamp");
        return DateTime.UtcNow;
    }
    
    /// <summary>
    /// Gets a sample data item
    /// </summary>
    /// <param name="id">The ID of the data item</param>
    /// <returns>A sample data object</returns>
    public object GetSampleData(int id)
    {
        _logger.LogInformation("Getting sample data for ID: {Id}", id);
        
        return new
        {
            Id = id,
            Name = $"Sample Item {id}",
            Description = $"This is a sample data item with ID {id}",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }
} 