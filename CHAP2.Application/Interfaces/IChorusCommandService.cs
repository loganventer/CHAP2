using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

public interface IChorusCommandService
{
    Task<Chorus> CreateChorusAsync(string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature, CancellationToken cancellationToken = default);
    Task<Chorus> UpdateChorusAsync(Guid id, string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature, CancellationToken cancellationToken = default);
    Task DeleteChorusAsync(Guid id, CancellationToken cancellationToken = default);
} 