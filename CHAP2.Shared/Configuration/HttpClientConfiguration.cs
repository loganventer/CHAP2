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
        });

        return services;
    }
}
