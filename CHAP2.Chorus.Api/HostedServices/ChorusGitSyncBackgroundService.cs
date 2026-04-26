using System.Globalization;
using CHAP2.Application.Interfaces;
using CHAP2.Chorus.Api.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Chorus.Api.HostedServices;

/// <summary>
/// Once-a-day background worker that delegates the actual work to
/// <see cref="IChorusGitSyncOrchestrator"/>. Owns nothing but the
/// schedule and the cancellation chain. No git knowledge, no disk
/// knowledge, no HTTP knowledge.
/// </summary>
public sealed class ChorusGitSyncBackgroundService : BackgroundService
{
    private readonly IChorusGitSyncOrchestrator _orchestrator;
    private readonly GitSyncOptions _options;
    private readonly ILogger<ChorusGitSyncBackgroundService> _logger;

    public ChorusGitSyncBackgroundService(
        IChorusGitSyncOrchestrator orchestrator,
        IOptions<GitSyncOptions> options,
        ILogger<ChorusGitSyncBackgroundService> logger)
    {
        _orchestrator = orchestrator;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("GitSync disabled — daily worker not scheduled");
            return;
        }
        if (!TimeOnly.TryParseExact(_options.ScheduleUtc, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fireAt))
        {
            _logger.LogError("GitSync ScheduleUtc {Value} is not HH:mm — worker not scheduled", _options.ScheduleUtc);
            return;
        }

        _logger.LogInformation("GitSync daily worker armed for {Time} UTC", fireAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = NextDelayUntil(fireAt, DateTime.UtcNow);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) { break; }

            await SyncSafely(stoppingToken);
        }

        // Final flush on graceful shutdown so pending edits aren't lost.
        await SyncSafely(CancellationToken.None);
    }

    private async Task SyncSafely(CancellationToken ct)
    {
        try
        {
            await _orchestrator.SyncNowAsync(progress: null, ct);
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            // Orchestrator already logs sanitized errors; swallow here so
            // the worker keeps running on the next cycle.
            _logger.LogWarning(ex, "GitSync cycle threw — will retry on the next schedule");
        }
    }

    /// <summary>
    /// Compute the wall-clock delay from <paramref name="now"/> until
    /// the next firing of <paramref name="fireAt"/> UTC. Always strictly
    /// in the future (rolls to tomorrow when the time has already
    /// passed today). Public so unit tests can drive it without owning
    /// the whole BackgroundService lifecycle.
    /// </summary>
    public static TimeSpan NextDelayUntil(TimeOnly fireAt, DateTime now)
    {
        var todayFire = new DateTime(now.Year, now.Month, now.Day, fireAt.Hour, fireAt.Minute, 0, DateTimeKind.Utc);
        if (todayFire > now) return todayFire - now;
        return todayFire.AddDays(1) - now;
    }
}
