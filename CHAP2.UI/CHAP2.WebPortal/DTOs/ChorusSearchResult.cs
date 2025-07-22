namespace CHAP2.WebPortal.DTOs;

public class ChorusSearchResult
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ChorusText { get; set; } = string.Empty;
    public int Key { get; set; }
    public int Type { get; set; }
    public int TimeSignature { get; set; }
    public DateTime CreatedAt { get; set; }
    public double Score { get; set; }
    public string Explanation { get; set; } = string.Empty;
} 