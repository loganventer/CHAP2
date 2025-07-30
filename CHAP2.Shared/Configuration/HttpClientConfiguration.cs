using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CHAP2.Shared.Configuration;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddCHAP2ApiClient(this IServiceCollection services, IConfiguration configuration)
    {
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
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        return services;
    }
} 