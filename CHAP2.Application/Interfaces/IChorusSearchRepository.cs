using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Search repository interface for Chorus entities following Interface Segregation Principle
/// </summary>
public interface IChorusSearchRepository
{
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
