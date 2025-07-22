using CHAP2.Console.Prompt.Configuration;
using CHAP2.Console.Prompt.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace CHAP2.Console.Prompt;

class Program
{
    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        
        try
        {
            var promptService = host.Services.GetRequiredService<IPromptService>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("Starting CHAP2 Prompt Console");
            logger.LogInformation("Type 'exit' to quit, 'search <query>' to search choruses, or ask a question");
            logger.LogInformation("Example: 'What choruses mention Jesus?' or 'search love'");
            logger.LogInformation("");

            while (true)
            {
                System.Console.Write("> ");
                var input = System.Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                    continue;
                
                if (input.ToLower() == "exit")
                    break;
                
                if (input.ToLower().StartsWith("search "))
                {
                    var query = input.Substring(7).Trim();
                    if (!string.IsNullOrEmpty(query))
                    {
                        try
                        {
                            var results = await promptService.SearchChorusesAsync(query);
                            DisplaySearchResults(results);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error searching choruses");
                            System.Console.WriteLine($"Error: {ex.Message}");
                        }
                    }
                }
                else
                {
                    try
                    {
                        var response = await promptService.AskQuestionAsync(input);
                        System.Console.WriteLine();
                        System.Console.WriteLine("Answer:");
                        System.Console.WriteLine(response);
                        System.Console.WriteLine();
                    }
                                            catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing question");
                            System.Console.WriteLine($"Error: {ex.Message}");
                        }
                }
            }
            
            logger.LogInformation("Prompt console stopped");
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Error in prompt console");
            Environment.Exit(1);
        }
    }

    static void DisplaySearchResults(List<CHAP2.Console.Prompt.DTOs.ChorusSearchResult> results)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"Found {results.Count} choruses:");
        System.Console.WriteLine();
        
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            System.Console.WriteLine($"{i + 1}. {result.Name} (Score: {result.Score:F3})");
            System.Console.WriteLine($"   Key: {result.Key}, Type: {result.Type}, Time: {result.TimeSignature}");
            System.Console.WriteLine($"   Text: {result.ChorusText}");
            System.Console.WriteLine($"   Created: {result.CreatedAt:yyyy-MM-dd}");
            System.Console.WriteLine();
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
                services.Configure<OllamaSettings>(
                    context.Configuration.GetSection("Ollama"));
                services.Configure<PromptSettings>(
                    context.Configuration.GetSection("Prompt"));

                // Register settings as singleton
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<QdrantSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<OllamaSettings>>().Value);
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<PromptSettings>>().Value);

                // Services
                services.AddScoped<IVectorSearchService, VectorSearchService>();
                services.AddHttpClient<IOllamaService, OllamaService>();
                services.AddScoped<IPromptService, PromptService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
} 