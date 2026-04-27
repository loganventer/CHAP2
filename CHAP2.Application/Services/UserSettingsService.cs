using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(
        IUserSettingsRepository repository,
        ICurrentUserService currentUser,
        ILogger<UserSettingsService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserSettings> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var existing = await _repository.GetAsync(userId, cancellationToken);
        if (existing is not null) return existing;

        _logger.LogDebug("No saved settings yet for {UserId}; returning empty default.", userId);
        return UserSettings.Default(userId);
    }

    public async Task<UserSettings> SaveMineAsync(string json, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var existing = await _repository.GetAsync(userId, cancellationToken) ?? UserSettings.Default(userId);
        existing.Update(json);
        await _repository.UpsertAsync(existing, cancellationToken);
        return existing;
    }
}
