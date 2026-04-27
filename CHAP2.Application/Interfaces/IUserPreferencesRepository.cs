using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}
