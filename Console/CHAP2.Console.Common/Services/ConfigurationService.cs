using CHAP2.Console.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CHAP2.Console.Common.Services;

/// <summary>
/// Configuration service implementation
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get configuration of specified type
    /// </summary>
    public T GetConfiguration<T>(string sectionName) where T : class, new()
    {
        var config = new T();
        _configuration.GetSection(sectionName).Bind(config);
        return config;
    }
} 