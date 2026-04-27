using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Infrastructure.Repositories;

public class UserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ApplicationDbContext _db;

    public UserPreferencesRepository(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<UserPreferences?> GetAsync(string userId, CancellationToken cancellationToken = default) =>
        _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<UserPreferences> UpsertAsync(UserPreferences preferences, CancellationToken cancellationToken = default)
    {
        var existing = await _db.UserPreferences.FindAsync(new object?[] { preferences.UserId }, cancellationToken);
        if (existing is null)
        {
            await _db.UserPreferences.AddAsync(preferences, cancellationToken);
        }
        else if (!ReferenceEquals(existing, preferences))
        {
            existing.Update(preferences.Theme, preferences.DefaultSearchScope, preferences.Language);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return existing ?? preferences;
    }
}
