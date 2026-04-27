using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface IUserSettingsApiService
{
    Task<UserSettingsDto?> GetMineAsync(CancellationToken cancellationToken = default);
    Task<UserSettingsDto?> SaveMineAsync(string json, CancellationToken cancellationToken = default);
}
