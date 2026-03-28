using CHAP2.WebPortal.DTOs;

namespace CHAP2.WebPortal.Services;

public class IntelligentSearchResult
{
    public List<ChorusSearchResult> SearchResults { get; set; } = new();
    public string AiAnalysis { get; set; } = string.Empty;
    public bool HasAiAnalysis { get; set; } = false;
    public string QueryUnderstanding { get; set; } = string.Empty;
}
