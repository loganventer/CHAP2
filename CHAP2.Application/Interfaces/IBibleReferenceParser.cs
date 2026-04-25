using CHAP2.Domain.ValueObjects;

namespace CHAP2.Application.Interfaces;

public interface IBibleReferenceParser
{
    Task<BibleReference?> TryParseAsync(string input, CancellationToken cancellationToken = default);
}
