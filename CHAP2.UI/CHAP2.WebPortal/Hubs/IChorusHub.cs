namespace CHAP2.WebPortal.Hubs;

/// <summary>
/// Defines the client-side methods that the server can invoke on connected clients.
/// Follows Dependency Inversion Principle: consumers depend on this abstraction.
/// </summary>
public interface IChorusHub
{
    /// <summary>
    /// Called when the currently displayed chorus changes on any connected client.
    /// </summary>
    Task ReceiveChorusChanged(string chorusId);

    /// <summary>
    /// Called when the key of the current chorus is changed from a remote client.
    /// </summary>
    Task ReceiveKeyChanged(string chorusId, string newKey);

    /// <summary>
    /// Called when a client broadcasts its current setlist so mobile
    /// sync clients can drive their prev/next navigation from it.
    /// Payload is a JSON array of chorus summaries with at least id + name.
    /// </summary>
    Task ReceiveSetlistUpdate(string setlistJson);
}
