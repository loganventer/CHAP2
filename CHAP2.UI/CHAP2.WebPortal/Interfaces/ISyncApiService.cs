namespace CHAP2.WebPortal.Interfaces;

/// <summary>
/// Open a streaming force-sync against the API and copy the raw SSE
/// bytes into <paramref name="destination"/> as they arrive. Mirrors
/// <see cref="IBibleApiService.StreamSearchAsync"/> -- one method, one
/// responsibility, no buffering.
/// </summary>
public interface ISyncApiService
{
    Task ForceSyncAsync(Stream destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-triggered server-side merge of the chorus-edits branch into
    /// main on GitHub. Returns the API's JSON outcome and HTTP status so
    /// the controller can map 409 (conflict) / 503 (disabled) correctly.
    /// </summary>
    Task<PromoteChorusOutcome> PromoteChorusAsync(CancellationToken cancellationToken = default);
}
