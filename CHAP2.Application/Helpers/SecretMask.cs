using System.Text.RegularExpressions;

namespace CHAP2.Application.Helpers;

/// <summary>
/// Strips PATs / inline credentials out of strings before they hit the
/// log or get returned to the user. Today only the GitHub HTTPS form
/// (https://x-access-token:TOKEN@github.com/...) but the matcher is
/// generous so future credential shapes don't need code changes.
/// </summary>
public static class SecretMask
{
    private static readonly Regex InlineCredential = new(
        @"(https?://)([^:@/\s]+):([^@/\s]+)@",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Apply(string? input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;
        return InlineCredential.Replace(input, "$1$2:***@");
    }
}
