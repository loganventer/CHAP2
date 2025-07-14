namespace CHAP2.Common.Interfaces;

public interface IRegexHelperService
{
    bool IsRegexMatch(string input, string pattern, bool caseInsensitive = true);
    List<string> ExtractMatches(string input, string pattern, bool caseInsensitive = true);
    bool IsValidRegexPattern(string pattern);
} 