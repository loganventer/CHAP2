using System.Text.RegularExpressions;
using CHAP2.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Common.Services;

public class RegexHelperService : IRegexHelperService
{
    private readonly ILogger<RegexHelperService> _logger;

    public RegexHelperService(ILogger<RegexHelperService> logger)
    {
        _logger = logger;
    }

    public bool IsRegexMatch(string input, string pattern, bool caseInsensitive = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pattern))
                return false;

            var options = caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
            return Regex.IsMatch(input, pattern, options);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during regex match for pattern: {Pattern}", pattern);
            return false;
        }
    }

    public List<string> ExtractMatches(string input, string pattern, bool caseInsensitive = true)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pattern))
                return new List<string>();

            var options = caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;
            var matches = Regex.Matches(input, pattern, options);
            
            return matches.Select(m => m.Value).ToList();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern: {Pattern}", pattern);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during regex extraction for pattern: {Pattern}", pattern);
            return new List<string>();
        }
    }

    public bool IsValidRegexPattern(string pattern)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return false;

            _ = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
} 