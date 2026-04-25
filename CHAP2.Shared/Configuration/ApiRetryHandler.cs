using System.Net;
using Microsoft.Extensions.Logging;

namespace CHAP2.Shared.Configuration;

/// <summary>
/// HttpClient delegating handler that retries transient failures with
/// exponential backoff. Built to absorb Render.com free-tier cold
/// starts where the chorus API can take 30-60s to wake from idle:
/// the first request after spin-down typically returns 502/503/504
/// or times out, and a retry shortly after succeeds.
///
/// Single responsibility: transport-level retry policy. No knowledge
/// of which endpoint is being called.
/// </summary>
public sealed class ApiRetryHandler : DelegatingHandler
{
    private readonly ILogger<ApiRetryHandler> _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _baseDelay;

    public ApiRetryHandler(ILogger<ApiRetryHandler> logger)
        : this(logger, SharedApiSettings.DefaultRetryAttempts, TimeSpan.FromMilliseconds(SharedApiSettings.DefaultRetryDelayMs))
    {
    }

    public ApiRetryHandler(ILogger<ApiRetryHandler> logger, int maxAttempts, TimeSpan baseDelay)
    {
        _logger = logger;
        _maxAttempts = Math.Max(1, maxAttempts);
        _baseDelay = baseDelay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        HttpResponseMessage? response = null;

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                if (!IsTransientStatus(response.StatusCode) || attempt == _maxAttempts)
                {
                    return response;
                }

                _logger.LogWarning(
                    "Transient HTTP status {Status} from {Url} (attempt {Attempt}/{Max}); retrying",
                    (int)response.StatusCode, request.RequestUri, attempt, _maxAttempts);

                response.Dispose();
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < _maxAttempts)
            {
                lastException = ex;
                _logger.LogWarning(ex,
                    "Transient HTTP failure calling {Url} (attempt {Attempt}/{Max}); retrying",
                    request.RequestUri, attempt, _maxAttempts);
            }

            await Task.Delay(BackoffFor(attempt), cancellationToken);
        }

        // Exhausted retries -- rethrow the last network exception, or
        // return the last response if the failures were status codes.
        if (lastException != null)
        {
            throw lastException;
        }
        return response!;
    }

    private TimeSpan BackoffFor(int attempt)
    {
        // 1x, 2x, 4x of base delay -- exponential.
        var multiplier = Math.Pow(2, attempt - 1);
        return TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * multiplier);
    }

    private static bool IsTransientStatus(HttpStatusCode status) =>
        status == HttpStatusCode.RequestTimeout
        || status == HttpStatusCode.BadGateway
        || status == HttpStatusCode.ServiceUnavailable
        || status == HttpStatusCode.GatewayTimeout
        || status == HttpStatusCode.TooManyRequests;

    private static bool IsTransientException(Exception ex) =>
        ex is HttpRequestException
        || ex is TaskCanceledException; // covers HttpClient timeouts
}
