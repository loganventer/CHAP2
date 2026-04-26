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
}
