using System.Globalization;
using System.Text;

namespace CHAP2.Application.Helpers;

/// <summary>
/// Shared text normalization used by Bible reference parsing and verse search.
/// Strips diacritics, lowercases, and either keeps only letters/digits
/// (identifier mode -- book lookup) or preserves single-space word
/// boundaries (search mode -- verse text matching).
/// </summary>
public static class BibleTextNormalizer
{
    /// <summary>
    /// Letters/digits only, lowercase, no diacritics. Used for matching
    /// book identifiers / names where punctuation and whitespace are noise.
    /// </summary>
    public static string Identifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Lowercase, no diacritics, single-space-separated words. Used for
    /// matching verse text where word boundaries matter (so "een geloof"
    /// can be split into ordered word matches).
    /// </summary>
    public static string SearchableText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(decomposed.Length);
        var lastWasSpace = true; // suppress leading whitespace
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasSpace = false;
            }
            else
            {
                // Treat any non-letter/digit (punctuation, whitespace, em-dashes)
                // as a word boundary, collapsed to a single space.
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
        }
        if (sb.Length > 0 && sb[^1] == ' ') sb.Length--;
        return sb.ToString();
    }

    /// <summary>
    /// Split a search query into normalized word tokens.
    /// </summary>
    public static string[] WordsFor(string? query)
    {
        var n = SearchableText(query);
        return n.Length == 0
            ? Array.Empty<string>()
            : n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
