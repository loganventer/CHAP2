using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public record SearchResult(
    IReadOnlyList<Chorus> Results,
    int TotalCount,
    string? Error = null,
    Dictionary<string, object>? Metadata = null
);
