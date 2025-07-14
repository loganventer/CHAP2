using System.Collections.Generic;
using System.Threading.Tasks;
using CHAP2.Common.Models;

namespace CHAP2.Common.Interfaces;

public interface IChorusResource
{
    Task AddChorusAsync(Chorus chorus);
    Task<IReadOnlyList<Chorus>> GetAllChorusesAsync();
} 