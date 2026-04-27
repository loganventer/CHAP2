using CHAP2.Shared.DTOs;

namespace CHAP2.WebPortal.Auth;

public sealed record ApiLoginOutcome(bool Succeeded, StoredTokens? Tokens, UserSummaryDto? User, IReadOnlyList<string> Errors)
{
    public static ApiLoginOutcome Ok(StoredTokens tokens, UserSummaryDto user) => new(true, tokens, user, Array.Empty<string>());
    public static ApiLoginOutcome Fail(params string[] errors) => new(false, null, null, errors);
}
