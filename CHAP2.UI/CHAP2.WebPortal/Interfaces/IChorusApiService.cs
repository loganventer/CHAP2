using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.WebPortal.Interfaces;

public interface IChorusApiService
{
    Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default);
    Task<List<Chorus>> SearchChorusesAsync(string searchTerm, string searchMode = "Contains", string searchIn = "all", CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default);
    Task<Chorus?> ConvertSlideAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> CreateChorusAsync(Chorus chorus, CancellationToken cancellationToken = default);
    Task<bool> UpdateChorusAsync(Guid id, string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature, CancellationToken cancellationToken = default);
    Task<bool> DeleteChorusAsync(string id, CancellationToken cancellationToken = default);
} 