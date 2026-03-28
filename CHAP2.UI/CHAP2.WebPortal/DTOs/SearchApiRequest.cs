namespace CHAP2.WebPortal.DTOs;

public record SearchApiRequest(
    string Query,
    string? SearchMode = "Contains",
    string? SearchScope = "All",
    int? MaxResults = 50
);
