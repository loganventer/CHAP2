using Microsoft.AspNetCore.SignalR;

namespace CHAP2.WebPortal.Hubs;

/// <summary>
/// SignalR hub for real-time chorus synchronization between devices.
/// Single responsibility: relay chorus change events between connected clients.
/// </summary>
public class ChorusHub : Hub<IChorusHub>
{
    private readonly ILogger<ChorusHub> _logger;

    /// <summary>
    /// The most recently displayed chorus ID. Static so it persists across connections.
    /// </summary>
    public static string? CurrentChorusId { get; private set; }

    public ChorusHub(ILogger<ChorusHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Broadcasts a chorus change to all other connected clients.
    /// Called by any client (desktop or mobile) when the displayed chorus changes.
    /// </summary>
    public async Task SendChorusChanged(string chorusId)
    {
        CurrentChorusId = chorusId;
        _logger.LogDebug("Chorus changed to {ChorusId} by connection {ConnectionId}", chorusId, Context.ConnectionId);
        await Clients.Others.ReceiveChorusChanged(chorusId);
    }

    /// <summary>
    /// Broadcasts a key change for the current chorus to all other connected clients.
    /// </summary>
    public async Task SendKeyChanged(string chorusId, string newKey)
    {
        _logger.LogDebug("Key changed to {NewKey} for chorus {ChorusId} by {ConnectionId}", newKey, chorusId, Context.ConnectionId);
        await Clients.Others.ReceiveKeyChanged(chorusId, newKey);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
