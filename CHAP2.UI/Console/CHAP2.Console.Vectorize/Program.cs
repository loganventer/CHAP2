using CHAP2.Console.Vectorize.Configuration;
using CHAP2.Console.Vectorize.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CHAP2.Console.Vectorize;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            var orchestrator = host.Services.GetRequiredService<IVectorizationOrchestrator>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            // Get data path from command line args or use default
            var dataPath = args.Length > 0 ? args[0] : "../../../CHAP2.Chorus.Api/data/chorus";
            
            logger.LogInformation("Starting chorus vectorization process");
            logger.LogInformation("Data path: {DataPath}", dataPath);
            
            await orchestrator.VectorizeChorusDataAsync(dataPath);
            
            logger.LogInformation("Vectorization process completed successfully");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error during vectorization process");
            Environment.Exit(1);
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory())
                      .AddJsonFile("appsettings.json", optional: false)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<QdrantSettings>(
                    context.Configuration.GetSection("Qdrant"));
                services.Configure<VectorizationSettings>(
                    context.Configuration.GetSection("Vectorization"));

                // Register settings as singleton
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<QdrantSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<VectorizationSettings>>().Value);

                // Services
                services.AddScoped<IChorusDataService, ChorusDataService>();
                services.AddScoped<IVectorizationService, FreeVectorizationService>();
                services.AddScoped<IQdrantService, QdrantService>();
                services.AddScoped<IVectorizationOrchestrator, VectorizationOrchestrator>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
} 