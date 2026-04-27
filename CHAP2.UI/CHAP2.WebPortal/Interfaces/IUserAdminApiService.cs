using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Interfaces;

public interface IUserAdminApiService
{
    Task<IReadOnlyList<UserSummaryDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task<UserSummaryDto?> RevokeRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
}
