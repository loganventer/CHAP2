using System.Text.RegularExpressions;
using System.Web;

namespace CHAP2.Application.Helpers;

/// <summary>
/// Provides methods for sanitizing user input to prevent XSS and injection attacks
/// </summary>
public static partial class InputSanitizer
{
    /// <summary>
    /// Sanitizes text input by encoding HTML entities and removing potentially dangerous content
    /// </summary>
    public static string SanitizeText(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // HTML encode to prevent XSS
        var sanitized = HttpUtility.HtmlEncode(input);

        // Remove any script-like patterns that might have been double-encoded
        sanitized = ScriptTagRegex().Replace(sanitized, string.Empty);

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes search query input
    /// </summary>
    public static string SanitizeSearchQuery(string? query)
    {
        if (string.IsNullOrEmpty(query))
            return string.Empty;

        // Remove control characters
        var sanitized = ControlCharsRegex().Replace(query, string.Empty);

        // Limit length to prevent DoS
        if (sanitized.Length > 500)
            sanitized = sanitized[..500];

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes a name field (chorus name, etc.)
    /// </summary>
    public static string SanitizeName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return string.Empty;

        // HTML encode
        var sanitized = HttpUtility.HtmlEncode(name);

        // Remove control characters
        sanitized = ControlCharsRegex().Replace(sanitized, string.Empty);

        // Limit length
        if (sanitized.Length > 200)
            sanitized = sanitized[..200];

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes chorus text content
    /// </summary>
    public static string SanitizeChorusText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // HTML encode
        var sanitized = HttpUtility.HtmlEncode(text);

        // Remove script tags
        sanitized = ScriptTagRegex().Replace(sanitized, string.Empty);

        // Remove control characters except newlines and tabs
        sanitized = DangerousControlCharsRegex().Replace(sanitized, string.Empty);

        return sanitized.Trim();
    }

    [GeneratedRegex(@"<script[^>]*>.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharsRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]")]
    private static partial Regex DangerousControlCharsRegex();
}
