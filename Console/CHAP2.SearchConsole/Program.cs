using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Services;
using CHAP2.Console.Common.Configuration;
using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;

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
                
                // Register IConfigurationService
                services.AddSingleton<IConfigurationService>(provider =>
                {
                    return new ConfigurationService(configuration);
                });
                
                // Configure settings
                services.Configure<ConsoleSettings>(
                    configuration.GetSection("ConsoleSettings"));
                services.Configure<ApiClientSettings>(
                    configuration.GetSection("ApiClientSettings"));
                services.Configure<ConsoleDisplaySettings>(
                    configuration.GetSection("ConsoleDisplaySettings"));
                services.Configure<ConsoleApiSettings>(
                    configuration.GetSection("ConsoleApiSettings"));
                
                // Register services
                services.AddSingleton<ISearchCacheService, MemorySearchCacheService>();
                services.AddScoped<IApiClientService, ApiClientService>();
                services.AddScoped<IConsoleDisplayService, ConsoleDisplayService>();
                services.AddScoped<IConsoleApplicationService, ConsoleApplicationService>();
                services.AddScoped<ISearchResultsObserver, ConsoleSearchResultsObserver>();
                services.AddScoped<ISelectionService, SelectionService>();
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
        // Register the observer
        var observer = scope.ServiceProvider.GetRequiredService<ISearchResultsObserver>();
        if (consoleService is ConsoleApplicationService concreteService)
        {
            concreteService.RegisterResultsObserver(observer);
        }

        try
        {
            // Always run search mode
            await RunSearchMode(configuration, consoleService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            System.Console.WriteLine($"Error: {ex.Message}");
            
            // Clear screen with delay before exiting on error
            if (consoleService is ConsoleApplicationService errorConcreteService)
            {
                errorConcreteService.ClearScreenWithDelay("An error occurred. Exiting...");
            }
        }
    }

    private static async Task RunSearchMode(IConfiguration configuration, IConsoleApplicationService consoleService, ILogger logger)
    {
        var searchDelayMs = configuration.GetValue<int>("SearchDelayMs", 300);
        var minSearchLength = configuration.GetValue<int>("MinSearchLength", 2);
        
        // Test API connectivity
        var isConnected = await consoleService.TestApiConnectivityAsync();
        if (!isConnected)
        {
            // Clear screen with delay before exiting
            if (consoleService is ConsoleApplicationService apiConcreteService)
            {
                apiConcreteService.ClearScreenWithDelay("API connection failed. Exiting...");
            }
            return;
        }

        // Run interactive search
        await consoleService.RunInteractiveSearchAsync(searchDelayMs, minSearchLength);
        
        // Clear screen with delay after normal completion
        if (consoleService is ConsoleApplicationService completionConcreteService)
        {
            completionConcreteService.ClearScreenWithDelay("Search completed. Goodbye!");
        }
    }
}
