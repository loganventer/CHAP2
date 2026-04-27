using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Infrastructure.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly ApplicationDbContext _db;

    public UserSettingsRepository(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<UserSettings?> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        _db.UserSettings.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<UserSettings> UpsertAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await _db.UserSettings.FindAsync(new object?[] { settings.UserId }, cancellationToken);
        if (existing is null)
        {
            await _db.UserSettings.AddAsync(settings, cancellationToken);
        }
        else if (!ReferenceEquals(existing, settings))
        {
            existing.Update(settings.Json);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return existing ?? settings;
    }
}
