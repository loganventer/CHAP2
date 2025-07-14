using System.Collections.Generic;
using System.Threading.Tasks;
using CHAP2.Common.Models;

namespace CHAP2.Common.Interfaces;

public interface IChorusResource
{
    Task AddChorusAsync(Chorus chorus);
    Task<IReadOnlyList<Chorus>> GetAllChorusesAsync();
    Task UpdateChorusAsync(Chorus chorus);
    Task<Chorus?> GetChorusByIdAsync(Guid id);
    Task<Chorus?> GetChorusByNameAsync(string name);
    Task<bool> ChorusExistsAsync(string name);
} 