using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Exceptions;

namespace CHAP2.Application.Services;

public class SetlistOwnershipPolicy : ISetlistOwnershipPolicy
{
    private readonly ICurrentUserService _currentUser;

    public SetlistOwnershipPolicy(ICurrentUserService currentUser)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
    }

    public void EnsureCanAccess(Setlist setlist)
    {
        if (setlist is null) throw new ArgumentNullException(nameof(setlist));
        if (_currentUser.IsAdmin) return;
        if (string.Equals(setlist.OwnerId, _currentUser.UserId, StringComparison.Ordinal)) return;
        throw new SetlistAccessDeniedException(setlist.Id, _currentUser.UserId);
    }
}
