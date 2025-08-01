using CHAP2.Domain.Entities;

namespace CHAP2.Console.Common.Interfaces;

public interface IApiClientService
{
    Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default);
    Task<Chorus?> ConvertSlideAsync(string filePath, CancellationToken cancellationToken = default);
    Task<List<Chorus>> SearchChorusesAsync(string searchTerm, string searchMode = "Contains", string searchIn = "all", CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default);
} 