using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;

namespace CHAP2.Application.Interfaces;

/// <summary>
/// Command record for creating a chorus
/// </summary>
public record CreateChorusCommand(
    string Name,
    string ChorusText,
    MusicalKey Key,
    ChorusType Type,
    TimeSignature TimeSignature);

/// <summary>
/// Command record for updating a chorus
/// </summary>
public record UpdateChorusCommand(
    Guid Id,
    string Name,
    string ChorusText,
    MusicalKey Key,
    ChorusType Type,
    TimeSignature TimeSignature);

public interface IChorusApplicationService
{
    Task<Chorus> CreateChorusAsync(CreateChorusCommand command);
    Task<Chorus> UpdateChorusAsync(UpdateChorusCommand command);
    Task DeleteChorusAsync(string id);
    Task<Chorus?> GetChorusByIdAsync(string id);
    Task<IEnumerable<Chorus>> SearchChorusesAsync(string query, string searchMode, string searchIn);
}
