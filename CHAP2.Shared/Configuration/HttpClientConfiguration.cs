using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace CHAP2.Shared.Configuration;

public static class HttpClientConfiguration
{
    public static IServiceCollection AddCHAP2ApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("CHAP2API", client =>
        {
            var apiBaseUrl = configuration[ConfigSections.ApiBaseUrl] ?? SharedApiSettings.DefaultApiBaseUrl;
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(SharedApiSettings.DefaultTimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });

        return services;
    }
} 