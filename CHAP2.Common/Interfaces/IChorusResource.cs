using System.Collections.Generic;
using System.Threading.Tasks;
using CHAP2.Common.Models;

namespace CHAP2.Common.Interfaces;

public interface IChorusResource
{
    Task AddChorusAsync(Chorus chorus, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default);
    Task UpdateChorusAsync(Chorus chorus, CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> ChorusExistsAsync(string name, CancellationToken cancellationToken = default);
} 