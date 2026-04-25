using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CHAP2.Shared.Configuration;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddCHAP2ApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Each delegating handler is its own transient service so it
        // can take an ILogger via DI and stay testable in isolation.
        services.AddTransient<ApiRetryHandler>();
        services.AddTransient<ApiCircuitBreakerHandler>();

        services.AddHttpClient("CHAP2API", client =>
        {
            // Check for environment variable first, then configuration, then default
            var apiBaseUrl = Environment.GetEnvironmentVariable("ApiService__BaseUrl")
                ?? configuration[ConfigSections.ApiBaseUrl]
                ?? SharedApiSettings.DefaultApiBaseUrl;

            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(SharedApiSettings.DefaultTimeoutSeconds);

            // Add headers for better debugging
            client.DefaultRequestHeaders.Add("User-Agent", "CHAP2-WebPortal/1.0");
        })
        // Resilience pipeline (outer-most first, so flow is
        //   client -> retry -> circuit-breaker -> primary):
        //   - Retry handles a single bad response with exponential backoff
        //     (absorbs Render free-tier cold-starts where the first
        //     request after spin-down often hits 502/503 or times out).
        //   - Circuit breaker trips after a run of consecutive failures,
        //     so subsequent user requests fail fast instead of every one
        //     waiting on a 60s cold start. After a cooldown it probes
        //     and closes when the API answers again.
        //   - "Lazy" by nature: HttpClient only opens a connection the
        //     first time we actually need to call the API.
        .AddHttpMessageHandler<ApiRetryHandler>()
        .AddHttpMessageHandler<ApiCircuitBreakerHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        // Recycle the underlying handler/connection pool more often than
        // the default 2 minutes so that when the API restarts behind
        // Render and gets a new internal IP, we don't hold a stale
        // connection that keeps failing.
        .SetHandlerLifetime(TimeSpan.FromSeconds(45));

        // Probe client -- shares the SAME circuit breaker as the search
        // pipeline (the breaker is process-wide via static state in
        // ApiCircuitBreakerHandler). A green probe therefore means
        // "the search client can actually call the API right now" --
        // not "the API answered some other endpoint".
        //
        // No retry handler: a probe should give a quick yes/no, not
        // loop for tens of seconds. Short timeout for the same reason.
        services.AddHttpClient("CHAP2API-Probe", client =>
        {
            var apiBaseUrl = Environment.GetEnvironmentVariable("ApiService__BaseUrl")
                ?? configuration[ConfigSections.ApiBaseUrl]
                ?? SharedApiSettings.DefaultApiBaseUrl;
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.Add("User-Agent", "CHAP2-WebPortal-Probe/1.0");
        })
        .AddHttpMessageHandler<ApiCircuitBreakerHandler>()
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        })
        .SetHandlerLifetime(TimeSpan.FromSeconds(30));

        return services;
    }
}
