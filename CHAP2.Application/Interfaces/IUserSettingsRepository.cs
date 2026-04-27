using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserSettings> UpsertAsync(UserSettings settings, CancellationToken cancellationToken = default);
}
