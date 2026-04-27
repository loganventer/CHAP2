using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferences> GetMineAsync(CancellationToken cancellationToken = default);
    Task<UserPreferences> UpdateMineAsync(Theme theme, SearchScope defaultSearchScope, Language language, CancellationToken cancellationToken = default);
}
