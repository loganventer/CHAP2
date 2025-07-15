using System;
using System.Collections.Generic;
using System.Linq;
using CHAP2.Common.Interfaces;

namespace CHAP2.Common.Services;

/// <summary>
/// Helper class for common configuration operations and patterns
/// </summary>
public static class ConfigurationHelper
{
    /// <summary>
    /// Gets a configuration value with environment variable fallback
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="configKey">The configuration key</param>
    /// <param name="envVarName">The environment variable name</param>
    /// <param name="defaultValue">Default value if neither config nor env var is found</param>
    /// <returns>The configuration value</returns>
    public static string GetConfigWithEnvFallback(IConfigurationService configService, string configKey, string envVarName, string defaultValue = "")
    {
        // Try configuration first
        var configValue = configService.GetString(configKey);
        if (!string.IsNullOrEmpty(configValue))
            return configValue;

        // Try environment variable
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (!string.IsNullOrEmpty(envValue))
            return envValue;

        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value with environment variable fallback for integers
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="configKey">The configuration key</param>
    /// <param name="envVarName">The environment variable name</param>
    /// <param name="defaultValue">Default value if neither config nor env var is found</param>
    /// <returns>The configuration value</returns>
    public static int GetIntConfigWithEnvFallback(IConfigurationService configService, string configKey, string envVarName, int defaultValue = 0)
    {
        // Try configuration first
        var configValue = configService.GetInt(configKey);
        if (configValue != 0 || configService.KeyExists(configKey))
            return configValue;

        // Try environment variable
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (int.TryParse(envValue, out var envInt))
            return envInt;

        return defaultValue;
    }

    /// <summary>
    /// Gets a configuration value with environment variable fallback for booleans
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="configKey">The configuration key</param>
    /// <param name="envVarName">The environment variable name</param>
    /// <param name="defaultValue">Default value if neither config nor env var is found</param>
    /// <returns>The configuration value</returns>
    public static bool GetBoolConfigWithEnvFallback(IConfigurationService configService, string configKey, string envVarName, bool defaultValue = false)
    {
        // Try configuration first
        var configValue = configService.GetBool(configKey);
        if (configValue != false || configService.KeyExists(configKey))
            return configValue;

        // Try environment variable
        var envValue = Environment.GetEnvironmentVariable(envVarName);
        if (bool.TryParse(envValue, out var envBool))
            return envBool;

        return defaultValue;
    }

    /// <summary>
    /// Validates required configuration sections exist
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="requiredSections">List of required section names</param>
    /// <returns>List of missing sections</returns>
    public static List<string> ValidateRequiredSections(IConfigurationService configService, params string[] requiredSections)
    {
        var missingSections = new List<string>();
        
        foreach (var section in requiredSections)
        {
            if (!configService.SectionExists(section))
            {
                missingSections.Add(section);
            }
        }
        
        return missingSections;
    }

    /// <summary>
    /// Validates required configuration keys exist
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="requiredKeys">List of required key names</param>
    /// <returns>List of missing keys</returns>
    public static List<string> ValidateRequiredKeys(IConfigurationService configService, params string[] requiredKeys)
    {
        var missingKeys = new List<string>();
        
        foreach (var key in requiredKeys)
        {
            if (!configService.KeyExists(key))
            {
                missingKeys.Add(key);
            }
        }
        
        return missingKeys;
    }

    /// <summary>
    /// Gets configuration values with validation
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="configKey">The configuration key</param>
    /// <param name="validator">Validation function</param>
    /// <param name="defaultValue">Default value if validation fails</param>
    /// <returns>The validated configuration value</returns>
    public static T GetValidatedConfig<T>(IConfigurationService configService, string configKey, Func<T, bool> validator, T defaultValue)
    {
        // This is a simplified version - in practice you'd need to implement type-specific methods
        // For now, we'll focus on string validation
        if (typeof(T) == typeof(string))
        {
            var value = configService.GetString(configKey);
            var validatorFunc = validator as Func<string, bool>;
            if (validatorFunc != null && validatorFunc(value))
            {
                return (T)(object)value;
            }
        }
        
        return defaultValue;
    }

    /// <summary>
    /// Merges multiple configuration sources with priority order
    /// </summary>
    /// <param name="configService">The configuration service</param>
    /// <param name="key">The configuration key</param>
    /// <param name="fallbackValues">Fallback values in order of priority</param>
    /// <returns>The first non-empty value found</returns>
    public static string GetConfigWithFallbacks(IConfigurationService configService, string key, params string[] fallbackValues)
    {
        // Try primary configuration
        var configValue = configService.GetString(key);
        if (!string.IsNullOrEmpty(configValue))
            return configValue;

        // Try fallback values in order
        foreach (var fallback in fallbackValues)
        {
            if (!string.IsNullOrEmpty(fallback))
                return fallback;
        }

        return string.Empty;
    }

    /// <summary>
    /// Gets a configuration section with default values
    /// </summary>
    /// <typeparam name="T">The configuration type</typeparam>
    /// <param name="configService">The configuration service</param>
    /// <param name="sectionName">The section name</param>
    /// <param name="defaultValues">Default values to apply if section doesn't exist</param>
    /// <returns>The configuration object with defaults applied</returns>
    public static T GetConfigurationWithDefaults<T>(IConfigurationService configService, string sectionName, T defaultValues) where T : class, new()
    {
        if (configService.SectionExists(sectionName))
        {
            return configService.GetConfiguration<T>(sectionName);
        }
        
        return defaultValues;
    }

    /// <summary>
    /// Converts a configuration value to a specific type with validation
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="configService">The configuration service</param>
    /// <param name="key">The configuration key</param>
    /// <param name="converter">Conversion function</param>
    /// <param name="defaultValue">Default value if conversion fails</param>
    /// <returns>The converted value</returns>
    public static T GetConvertedConfig<T>(IConfigurationService configService, string key, Func<string, T> converter, T defaultValue)
    {
        var stringValue = configService.GetString(key);
        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;

        try
        {
            return converter(stringValue);
        }
        catch
        {
            return defaultValue;
        }
    }
} 