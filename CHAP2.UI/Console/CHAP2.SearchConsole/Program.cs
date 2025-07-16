using Microsoft.Extensions.Logging;
using CHAP2.Console.Common;
using CHAP2.Console.Common.Interfaces;
using CHAP2.Console.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CHAP2.SearchConsole;

class Program
{
    static async Task Main(string[] args)
    {
        var host = ConsoleHostBuilder.Build(services =>
        {
            services.AddSingleton<ISearchCacheService, MemorySearchCacheService>();
            services.AddScoped<IApiClientService, ApiClientService>();
            services.AddScoped<IConsoleDisplayService, ConsoleDisplayService>();
            services.AddScoped<IConsoleApplicationService, ConsoleApplicationService>();
            services.AddScoped<ISearchResultsObserver, ConsoleSearchResultsObserver>();
            services.AddScoped<ISelectionService, SelectionService>();
            services.AddScoped<IConfigurationService, ConfigurationService>();
        });

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        using var scope = host.Services.CreateScope();
        var consoleService = scope.ServiceProvider.GetRequiredService<IConsoleApplicationService>();
        var observer = scope.ServiceProvider.GetRequiredService<ISearchResultsObserver>();
        if (consoleService is ConsoleApplicationService concreteService)
        {
            concreteService.RegisterResultsObserver(observer);
        }

        try
        {
            await RunSearchMode(consoleService, logger);
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

    private static async Task RunSearchMode(IConsoleApplicationService consoleService, ILogger logger)
    {
        // These could be injected/configured as needed
        var searchDelayMs = 300;
        var minSearchLength = 2;

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
