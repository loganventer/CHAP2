namespace CHAP2.Shared.Configuration;

public class SearchSettings
{
    public int MaxResults { get; set; } = 50;
    public int MaxDisplayResults { get; set; } = 10;
    public string DefaultSearchMode { get; set; } = "Contains";
    public string DefaultSearchScope { get; set; } = "all";
}
