using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface IUserPreferencesApiService
{
    Task<UserPreferencesDto?> GetMineAsync(CancellationToken cancellationToken = default);
    Task<UserPreferencesDto?> UpdateMineAsync(string theme, string defaultSearchScope, string language, CancellationToken cancellationToken = default);
}
