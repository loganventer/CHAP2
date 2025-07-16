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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
                System.Console.WriteLine($"Configuring HttpClient with BaseAddress: {apiBaseUrl}");
                
                services.AddHttpClient("CHAP2API", client =>
                {
                    var baseUri = new Uri(apiBaseUrl);
                    client.BaseAddress = baseUri;
                    client.Timeout = TimeSpan.FromSeconds(30);
                    System.Console.WriteLine($"HttpClient BaseAddress set to: {client.BaseAddress}");
                });
                
                services.AddSingleton<IConfigurationService>(provider =>
                {
                    return new ConfigurationService(configuration);
                });
                
                services.Configure<ConsoleSettings>(
                    configuration.GetSection("ConsoleSettings"));
                services.Configure<ApiClientSettings>(
                    configuration.GetSection("ApiClientSettings"));
                services.Configure<ConsoleDisplaySettings>(
                    configuration.GetSection("ConsoleDisplaySettings"));
                services.Configure<ConsoleApiSettings>(
                    configuration.GetSection("ConsoleApiSettings"));
                
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
        var observer = scope.ServiceProvider.GetRequiredService<ISearchResultsObserver>();
        if (consoleService is ConsoleApplicationService concreteService)
        {
            concreteService.RegisterResultsObserver(observer);
        }

        try
        {
            await RunSearchMode(configuration, consoleService, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            System.Console.WriteLine($"Error: {ex.Message}");
            
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
        
        var isConnected = await consoleService.TestApiConnectivityAsync();
        if (!isConnected)
        {
            if (consoleService is ConsoleApplicationService apiConcreteService)
            {
                apiConcreteService.ClearScreenWithDelay("API connection failed. Exiting...");
            }
            return;
        }

        await consoleService.RunInteractiveSearchAsync(searchDelayMs, minSearchLength);
        
        if (consoleService is ConsoleApplicationService completionConcreteService)
        {
            completionConcreteService.ClearScreenWithDelay();
        }
    }
}
