using CHAP2.Domain.Entities;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Write-only repository interface for Chorus entities following Interface Segregation Principle
/// </summary>
public interface IChorusWriteRepository
{
    /// <summary>
    /// Adds a new chorus to the repository
    /// </summary>
    Task<Chorus> AddAsync(Chorus chorus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing chorus in the repository
    /// </summary>
    Task<Chorus> UpdateAsync(Chorus chorus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a chorus by its ID
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
