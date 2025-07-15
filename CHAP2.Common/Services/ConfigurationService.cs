using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using CHAP2.Common.Interfaces;

namespace CHAP2.Common.Services;

/// <summary>
/// Generic configuration service that provides type-safe access to configuration sections
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<Type, object> _cachedConfigurations;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cachedConfigurations = new Dictionary<Type, object>();
    }

    /// <summary>
    /// Gets a configuration section and binds it to the specified type
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration to</typeparam>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>The bound configuration object</returns>
    public T GetConfiguration<T>(string sectionName) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

        var configType = typeof(T);
        
        // Check if we have a cached configuration
        if (_cachedConfigurations.TryGetValue(configType, out var cachedConfig))
        {
            return (T)cachedConfig;
        }

        // Create new configuration instance
        var configuration = new T();
        _configuration.GetSection(sectionName).Bind(configuration);
        
        // Cache the configuration
        _cachedConfigurations[configType] = configuration;
        
        return configuration;
    }

    /// <summary>
    /// Gets a configuration value as a string
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    public string GetString(string key, string defaultValue = "")
    {
        return _configuration[key] ?? defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as an integer
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (int.TryParse(_configuration[key], out var value))
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a boolean
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    public bool GetBool(string key, bool defaultValue = false)
    {
        if (bool.TryParse(_configuration[key], out var value))
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a double
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    public double GetDouble(string key, double defaultValue = 0.0)
    {
        if (double.TryParse(_configuration[key], out var value))
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as a TimeSpan
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    public TimeSpan GetTimeSpan(string key, TimeSpan defaultValue = default)
    {
        if (TimeSpan.TryParse(_configuration[key], out var value))
            return value;
        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value as an array of strings
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    public string[] GetStringArray(string key, string[] defaultValue = null)
    {
        var section = _configuration.GetSection(key);
        if (section.Exists())
        {
            return section.Get<string[]>() ?? defaultValue ?? Array.Empty<string>();
        }
        return defaultValue ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets a configuration value as a dictionary
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    public Dictionary<string, string> GetDictionary(string key, Dictionary<string, string> defaultValue = null)
    {
        var section = _configuration.GetSection(key);
        if (section.Exists())
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var child in section.GetChildren())
            {
                dictionary[child.Key] = child.Value ?? string.Empty;
            }
            return dictionary;
        }
        return defaultValue ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Checks if a configuration section exists
    /// </summary>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>True if the section exists, false otherwise</returns>
    public bool SectionExists(string sectionName)
    {
        return _configuration.GetSection(sectionName).Exists();
    }

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    public bool KeyExists(string key)
    {
        return !string.IsNullOrEmpty(_configuration[key]);
    }

    /// <summary>
    /// Gets all configuration keys that start with the specified prefix
    /// </summary>
    /// <param name="prefix">The prefix to search for</param>
    /// <returns>Array of configuration keys</returns>
    public string[] GetKeysWithPrefix(string prefix)
    {
        var keys = new List<string>();
        var section = _configuration.GetSection(prefix);
        
        if (section.Exists())
        {
            GetKeysRecursive(section, prefix, keys);
        }
        
        return keys.ToArray();
    }

    private void GetKeysRecursive(IConfigurationSection section, string currentPath, List<string> keys)
    {
        foreach (var child in section.GetChildren())
        {
            var childPath = string.IsNullOrEmpty(currentPath) ? child.Key : $"{currentPath}:{child.Key}";
            
            if (child.Value != null)
            {
                keys.Add(childPath);
            }
            else
            {
                GetKeysRecursive(child, childPath, keys);
            }
        }
    }

    /// <summary>
    /// Reloads the configuration from the source
    /// </summary>
    public void Reload()
    {
        _cachedConfigurations.Clear();
        if (_configuration is IConfigurationRoot configurationRoot)
        {
            configurationRoot.Reload();
        }
    }
} 