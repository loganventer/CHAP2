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
}
