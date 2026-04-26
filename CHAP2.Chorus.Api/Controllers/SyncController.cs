using System.Text.Json;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Models;
using CHAP2.Chorus.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CHAP2.Chorus.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class SyncController : ChapControllerAbstractBase
{
    private readonly IChorusGitSyncOrchestrator _orchestrator;
    private readonly GitSyncOptions _options;

    public SyncController(
        ILogger<SyncController> logger,
        IChorusGitSyncOrchestrator orchestrator,
        IOptions<GitSyncOptions> options) : base(logger)
    {
        _orchestrator = orchestrator;
        _options = options.Value;
    }

    /// <summary>
    /// Force an immediate chorus git sync. Streams progress events as
    /// SSE so a "Sync now" UI can render a progress bar. Closes the
    /// stream with `event: done`.
    /// </summary>
    [HttpPost("force")]
    public async Task Force(CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";

        if (!_options.Enabled)
        {
            Response.StatusCode = 503;
            await Response.WriteAsync("event: error\ndata: {\"error\":\"git-sync-disabled\"}\n\n", cancellationToken);
            return;
        }

        LogAction("ForceSync");
        var json = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        var progress = new Progress<ChorusGitSyncProgress>(async p =>
        {
            try
            {
                var payload = JsonSerializer.Serialize(p, json);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ForceSync progress write failed (client likely disconnected)");
            }
        });

        try
        {
            var result = await _orchestrator.SyncNowAsync(progress, cancellationToken);
            var doneJson = JsonSerializer.Serialize(result, json);
            await Response.WriteAsync($"event: done\ndata: {doneJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException) { /* client disconnected */ }
    }
}
