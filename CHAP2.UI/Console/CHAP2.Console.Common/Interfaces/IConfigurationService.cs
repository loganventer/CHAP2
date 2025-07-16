namespace CHAP2.Console.Common.Interfaces;

/// <summary>
/// Interface for configuration service
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Get configuration of specified type
    /// </summary>
    T GetConfiguration<T>(string sectionName) where T : class, new();
} 