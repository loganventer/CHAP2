using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace CHAP2.Infrastructure.Repositories;

public class SetlistRepository : ISetlistRepository
{
    private readonly ApplicationDbContext _db;

    public SetlistRepository(ApplicationDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public Task<Setlist?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Setlists
            .Include(nameof(Setlist.Items))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Setlist>> GetByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Setlists
            .Include(nameof(Setlist.Items))
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows;
    }

    public Task<Setlist?> GetWorkingDraftAsync(string ownerId, CancellationToken cancellationToken = default) =>
        _db.Setlists
            .Include(nameof(Setlist.Items))
            .FirstOrDefaultAsync(s => s.OwnerId == ownerId && s.IsWorkingDraft, cancellationToken);

    public async Task<Setlist> AddAsync(Setlist setlist, CancellationToken cancellationToken = default)
    {
        await _db.Setlists.AddAsync(setlist, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return setlist;
    }

    public async Task<Setlist> UpdateAsync(Setlist setlist, CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
        return setlist;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Setlists.FindAsync(new object?[] { id }, cancellationToken);
        if (entity is null) return;
        _db.Setlists.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
