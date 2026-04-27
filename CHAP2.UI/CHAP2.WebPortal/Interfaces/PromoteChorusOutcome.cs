namespace CHAP2.WebPortal.Interfaces;

/// <summary>
/// Pass-through wrapper of the API's promote-chorus response so the
/// bridge controller can preserve the upstream HTTP status code (409 for
/// merge conflict, 503 for disabled, 200/202 for success).
/// </summary>
public sealed record PromoteChorusOutcome(int StatusCode, string Body);
