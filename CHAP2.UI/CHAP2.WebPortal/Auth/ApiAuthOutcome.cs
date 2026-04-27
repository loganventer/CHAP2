namespace CHAP2.WebPortal.Auth;

public sealed record ApiAuthOutcome(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static ApiAuthOutcome Ok() => new(true, Array.Empty<string>());
    public static ApiAuthOutcome Fail(params string[] errors) => new(false, errors);
}
