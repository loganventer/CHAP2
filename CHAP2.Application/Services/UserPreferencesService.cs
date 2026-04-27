using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly IUserPreferencesRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(
        IUserPreferencesRepository repository,
        ICurrentUserService currentUser,
        ILogger<UserPreferencesService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserPreferences> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var existing = await _repository.GetAsync(userId, cancellationToken);
        if (existing is not null) return existing;

        _logger.LogInformation("No preferences yet for {UserId}; returning defaults", userId);
        return UserPreferences.Default(userId);
    }

    public async Task<UserPreferences> UpdateMineAsync(
        Theme theme,
        SearchScope defaultSearchScope,
        Language language,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var existing = await _repository.GetAsync(userId, cancellationToken) ?? UserPreferences.Default(userId);
        existing.Update(theme, defaultSearchScope, language);
        await _repository.UpsertAsync(existing, cancellationToken);
        return existing;
    }
}
