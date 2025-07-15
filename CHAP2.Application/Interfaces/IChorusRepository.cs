using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Repository interface for Chorus entities following IDesign principles
/// </summary>
public interface IChorusRepository
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
    /// Gets multiple choruses by their IDs
    /// </summary>
    Task<IReadOnlyList<Chorus>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    
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
    
    /// <summary>
    /// Checks if a chorus exists by name
    /// </summary>
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a chorus exists by ID
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches choruses by text content
    /// </summary>
    Task<IReadOnlyList<Chorus>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets choruses by musical key
    /// </summary>
    Task<IReadOnlyList<Chorus>> GetByKeyAsync(MusicalKey key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets choruses by time signature
    /// </summary>
    Task<IReadOnlyList<Chorus>> GetByTimeSignatureAsync(TimeSignature timeSignature, CancellationToken cancellationToken = default);
} 