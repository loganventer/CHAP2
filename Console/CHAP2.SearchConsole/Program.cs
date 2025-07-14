using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Services;
using CHAP2.Console.Common.Configuration;

namespace CHAP2.SearchConsole;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
                System.Console.WriteLine($"Configuring HttpClient with BaseAddress: {apiBaseUrl}");
                
                // Add HttpClient with explicit configuration
                services.AddHttpClient("CHAP2API", client =>
                {
                    var baseUri = new Uri(apiBaseUrl);
                    client.BaseAddress = baseUri;
                    client.Timeout = TimeSpan.FromSeconds(30);
                    System.Console.WriteLine($"HttpClient BaseAddress set to: {client.BaseAddress}");
                });
                
                // Configure settings
                services.Configure<ConsoleSettings>(
                    configuration.GetSection("ConsoleSettings"));
                services.Configure<ApiClientSettings>(
                    configuration.GetSection("ApiClientSettings"));
                
                // Register services
                services.AddScoped<IApiClientService, ApiClientService>();
                services.AddScoped<IConsoleApplicationService, ConsoleApplicationService>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        using var scope = host.Services.CreateScope();
        
        // Debug: Test HttpClient creation
        try
        {
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            using var testClient = httpClientFactory.CreateClient("CHAP2API");
            System.Console.WriteLine($"Debug: HttpClient BaseAddress after creation: {testClient.BaseAddress}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Debug: Error creating HttpClient: {ex.Message}");
        }
        
        var consoleService = scope.ServiceProvider.GetRequiredService<IConsoleApplicationService>();

        try
        {
            // Always run search mode
            await RunSearchMode(configuration, consoleService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task RunSearchMode(IConfiguration configuration, IConsoleApplicationService consoleService, ILogger logger)
    {
        var searchDelayMs = configuration.GetValue<int>("SearchDelayMs", 300);
        var minSearchLength = configuration.GetValue<int>("MinSearchLength", 2);
        
        System.Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
        System.Console.WriteLine("=============================================");
        System.Console.WriteLine($"API Base URL: {configuration["ApiBaseUrl"]}");
        System.Console.WriteLine($"Search Delay: {searchDelayMs}ms");
        System.Console.WriteLine($"Minimum Search Length: {minSearchLength}");
        System.Console.WriteLine();

        // Test API connectivity
        var isConnected = await consoleService.TestApiConnectivityAsync();
        if (!isConnected)
        {
            return;
        }

        System.Console.WriteLine("API is accessible. Starting interactive search...\n");

        // Run interactive search
        await consoleService.RunInteractiveSearchAsync(searchDelayMs, minSearchLength);
    }
}
