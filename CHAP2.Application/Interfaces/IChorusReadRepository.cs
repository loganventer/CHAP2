using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Read-only repository interface for Chorus entities following Interface Segregation Principle
/// </summary>
public interface IChorusReadRepository
{
    /// <summary>
    /// Gets a chorus by its unique identifier
    /// </summary>
    Task<Chorus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chorus by its name (case-insensitive)
    /// </summary>
    Task<Chorus?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all choruses
    /// </summary>
    Task<IReadOnlyList<Chorus>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets choruses with pagination support
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Maximum number of records to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of choruses</returns>
    Task<IReadOnlyList<Chorus>> GetAllAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of choruses
    /// </summary>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple choruses by their IDs
    /// </summary>
    Task<IReadOnlyList<Chorus>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a chorus exists by name
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a chorus exists by ID
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}
