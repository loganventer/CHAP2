namespace CHAP2API.Configuration;

public class SearchSettings
{
    public string DefaultSearchMode { get; set; } = "Contains";
    public string DefaultSearchScope { get; set; } = "all";
    public int MaxSearchResults { get; set; } = 100;
    public bool CaseInsensitiveSearch { get; set; } = true;
} 