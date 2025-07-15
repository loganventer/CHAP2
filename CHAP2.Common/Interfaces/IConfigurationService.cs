using System;
using System.Collections.Generic;

namespace CHAP2.Common.Interfaces;

/// <summary>
/// Generic configuration service interface that provides type-safe access to configuration sections
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration section and binds it to the specified type
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration to</typeparam>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>The bound configuration object</returns>
    T GetConfiguration<T>(string sectionName) where T : class, new();

    /// <summary>
    /// Gets a configuration value as a string
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    string GetString(string key, string defaultValue = "");

    /// <summary>
    /// Gets a configuration value as an integer
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    int GetInt(string key, int defaultValue = 0);

    /// <summary>
    /// Gets a configuration value as a boolean
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    bool GetBool(string key, bool defaultValue = false);

    /// <summary>
    /// Gets a configuration value as a double
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    double GetDouble(string key, double defaultValue = 0.0);

    /// <summary>
    /// Gets a configuration value as a TimeSpan
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found or invalid</param>
    /// <returns>The configuration value or default</returns>
    TimeSpan GetTimeSpan(string key, TimeSpan defaultValue = default);

    /// <summary>
    /// Gets a configuration value as an array of strings
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    string[] GetStringArray(string key, string[] defaultValue = null);

    /// <summary>
    /// Gets a configuration value as a dictionary
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>The configuration value or default</returns>
    Dictionary<string, string> GetDictionary(string key, Dictionary<string, string> defaultValue = null);

    /// <summary>
    /// Checks if a configuration section exists
    /// </summary>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>True if the section exists, false otherwise</returns>
    bool SectionExists(string sectionName);

    /// <summary>
    /// Checks if a configuration key exists
    /// </summary>
    /// <param name="key">The configuration key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    bool KeyExists(string key);

    /// <summary>
    /// Gets all configuration keys that start with the specified prefix
    /// </summary>
    /// <param name="prefix">The prefix to search for</param>
    /// <returns>Array of configuration keys</returns>
    string[] GetKeysWithPrefix(string prefix);

    /// <summary>
    /// Reloads the configuration from the source
    /// </summary>
    void Reload();
} 