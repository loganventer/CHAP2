using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public interface IChorusQueryService
{
    Task<IReadOnlyList<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chorus>> SearchChorusesAsync(string searchTerm, SearchMode searchMode, SearchScope searchScope, CancellationToken cancellationToken = default);
} 