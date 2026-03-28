using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public record SearchRequest(
    string Query,
    SearchMode Mode = SearchMode.Contains,
    SearchScope Scope = SearchScope.All,
    int MaxResults = 50,
    bool UseAi = false
);
