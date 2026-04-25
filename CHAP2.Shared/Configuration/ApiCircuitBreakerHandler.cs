using System.Net;
using Microsoft.Extensions.Logging;

namespace CHAP2.Shared.Configuration;

/// <summary>
/// HttpClient delegating handler that short-circuits requests after a
/// run of consecutive failures, then probes the API after a cooldown
/// to decide whether it's recovered. Stops every user request hanging
/// on a 60-second cold-start when the API is genuinely down.
///
/// State machine:
///   Closed    : pass requests through, count failures
///   Open      : reject requests immediately for OpenDuration
///   HalfOpen  : allow ONE probe; success -> Closed, failure -> Open
///
/// Cross-request state lives in static fields because HttpClientFactory
/// creates a fresh handler per request -- the handler IS the breaker
/// for the named client lifetime.
///
/// Single responsibility: trip-and-recover policy. No retry logic.
/// Use together with ApiRetryHandler in the order:
///   client -> retry -> breaker -> primary
/// (retry sits outside breaker so each retry attempt is gated by the
/// breaker, the standard pattern).
/// </summary>
public sealed class ApiCircuitBreakerHandler : DelegatingHandler
{
    private enum State { Closed, Open, HalfOpen }

    private static readonly object _gate = new();
    private static State _state = State.Closed;
    private static int _consecutiveFailures = 0;
    private static DateTime _openUntilUtc = DateTime.MinValue;

    private readonly ILogger<ApiCircuitBreakerHandler> _logger;
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;

    public ApiCircuitBreakerHandler(ILogger<ApiCircuitBreakerHandler> logger)
        : this(logger, failureThreshold: 3, openDuration: TimeSpan.FromSeconds(20))
    {
    }

    public ApiCircuitBreakerHandler(ILogger<ApiCircuitBreakerHandler> logger, int failureThreshold, TimeSpan openDuration)
    {
        _logger = logger;
        _failureThreshold = Math.Max(1, failureThreshold);
        _openDuration = openDuration;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        EnforceGate();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (IsTransientStatus(response.StatusCode))
            {
                RecordFailure($"HTTP {(int)response.StatusCode}");
            }
            else
            {
                RecordSuccess();
            }
            return response;
        }
        catch (Exception ex) when (IsTransientException(ex))
        {
            RecordFailure(ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// If the breaker is Open, either flip to HalfOpen (cooldown elapsed)
    /// or fail fast. Closed and HalfOpen states pass straight through.
    /// </summary>
    private void EnforceGate()
    {
        lock (_gate)
        {
            if (_state == State.Open)
            {
                if (DateTime.UtcNow >= _openUntilUtc)
                {
                    _state = State.HalfOpen;
                    _logger.LogInformation("Circuit half-open: probing chorus API recovery");
                }
                else
                {
                    var wait = (_openUntilUtc - DateTime.UtcNow).TotalSeconds;
                    throw new HttpRequestException(
                        $"Chorus API circuit is open; retry in {Math.Ceiling(wait)}s");
                }
            }
        }
    }

    private void RecordSuccess()
    {
        lock (_gate)
        {
            if (_state != State.Closed)
            {
                _logger.LogInformation("Circuit closed: chorus API responsive again");
            }
            _state = State.Closed;
            _consecutiveFailures = 0;
        }
    }

    private void RecordFailure(string reason)
    {
        lock (_gate)
        {
            _consecutiveFailures++;
            // A failed probe in HalfOpen reopens immediately; otherwise
            // we open once we cross the failure threshold.
            if (_state == State.HalfOpen || _consecutiveFailures >= _failureThreshold)
            {
                _state = State.Open;
                _openUntilUtc = DateTime.UtcNow + _openDuration;
                _logger.LogWarning(
                    "Circuit OPEN until {Until:O} after {Failures} consecutive failures (last: {Reason})",
                    _openUntilUtc, _consecutiveFailures, reason);
            }
        }
    }

    private static bool IsTransientStatus(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout
        || status == HttpStatusCode.BadGateway
        || status == HttpStatusCode.ServiceUnavailable
        || status == HttpStatusCode.GatewayTimeout
        || status == HttpStatusCode.TooManyRequests;

    private static bool IsTransientException(Exception ex) =>
        ex is HttpRequestException
        || ex is TaskCanceledException;
}
