namespace CHAP2.Chorus.Api.Interfaces;

/// <summary>
/// Interface for application services that can be injected via dependency injection
/// </summary>
public interface IServices
{
    /// <summary>
    /// Gets the current application name
    /// </summary>
    /// <returns>The application name</returns>
    string GetApplicationName();
    
    /// <summary>
    /// Gets the current timestamp
    /// </summary>
    /// <returns>The current timestamp</returns>
    DateTime GetCurrentTimestamp();
    
    /// <summary>
    /// Gets a sample data item
    /// </summary>
    /// <param name="id">The ID of the data item</param>
    /// <returns>A sample data object</returns>
    object GetSampleData(int id);
} 