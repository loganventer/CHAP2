using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

public class SetlistQueryService : ISetlistQueryService
{
    private readonly ISetlistRepository _repository;
    private readonly ISetlistOwnershipPolicy _ownership;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SetlistQueryService> _logger;

    public SetlistQueryService(
        ISetlistRepository repository,
        ISetlistOwnershipPolicy ownership,
        ICurrentUserService currentUser,
        ILogger<SetlistQueryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _ownership = ownership ?? throw new ArgumentNullException(nameof(ownership));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<Setlist>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var ownerId = _currentUser.UserId;
        _logger.LogInformation("Loading setlists for owner {OwnerId}", ownerId);
        return await _repository.GetByOwnerAsync(ownerId, cancellationToken);
    }

    public async Task<Setlist?> GetByIdAsync(Guid setlistId, CancellationToken cancellationToken = default)
    {
        var setlist = await _repository.GetByIdAsync(setlistId, cancellationToken);
        if (setlist is null) return null;
        _ownership.EnsureCanAccess(setlist);
        return setlist;
    }
}
