using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettings> GetMineAsync(CancellationToken cancellationToken = default);
    Task<UserSettings> SaveMineAsync(string json, CancellationToken cancellationToken = default);
}
