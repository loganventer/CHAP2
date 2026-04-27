using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

public interface ISetlistOwnershipPolicy
{
    void EnsureCanAccess(Setlist setlist);
}
