using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CHAP2.Common.Models;

namespace CHAP2.SearchConsole;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddCommandLine(args)
            .Build();

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient("CHAP2API", client =>
                {
                    client.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "http://localhost:5000");
                });
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var httpClientFactory = host.Services.GetRequiredService<IHttpClientFactory>();

        try
        {
            // Check if user wants to convert a file or just search
            var mode = args.Length > 0 && args[0].ToLower() == "convert" ? "convert" : "search";
            
            if (mode == "convert")
            {
                await RunSlideConversionMode(args, configuration, httpClientFactory, logger);
            }
            else
            {
                await RunSearchMode(configuration, httpClientFactory, logger);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task RunSearchMode(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        var searchDelayMs = configuration.GetValue<int>("SearchDelayMs", 300);
        var minSearchLength = configuration.GetValue<int>("MinSearchLength", 2);
        
        Console.WriteLine("CHAP2 Search Console - Interactive Search Mode");
        Console.WriteLine("=============================================");
        Console.WriteLine($"API Base URL: {configuration["ApiBaseUrl"]}");
        Console.WriteLine($"Search Delay: {searchDelayMs}ms");
        Console.WriteLine($"Minimum Search Length: {minSearchLength}");
        Console.WriteLine();

        using var httpClient = httpClientFactory.CreateClient("CHAP2API");
        
        // Test API connectivity
        Console.WriteLine("Testing API connectivity...");
        try
        {
            var healthResponse = await httpClient.GetAsync("/api/health/ping");
            Console.WriteLine($"Health check status: {healthResponse.StatusCode}");
            if (!healthResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("API is not accessible. Make sure the API is running.");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check failed: {ex.Message}");
            Console.WriteLine("Make sure the API is running on the configured URL");
            return;
        }

        Console.WriteLine("API is accessible. Starting interactive search...\n");
        Console.WriteLine("Type to search choruses. Search triggers automatically.");
        Console.WriteLine("Press Enter to select, Escape to clear, Ctrl+C to exit.\n");

        var searchString = "";
        var currentResults = new List<Chorus>();
        var searchCancellationTokenSource = new CancellationTokenSource();

        Console.Write("Search: ");

        while (true)
        {
            var key = Console.ReadKey(true);
            
            if (key.Key == ConsoleKey.Enter)
            {
                if (currentResults.Count == 1)
                {
                    Console.WriteLine("\n=== SINGLE CHORUS FOUND ===");
                    DisplayChorus(currentResults[0]);
                    break;
                }
                else if (currentResults.Count > 1)
                {
                    Console.WriteLine($"\nMultiple choruses found ({currentResults.Count}). Continue typing to narrow down.");
                    Console.Write("Search: " + searchString);
                }
                continue;
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                searchString = "";
                currentResults.Clear();
                Console.WriteLine("\nSearch cleared.");
                Console.Write("Search: ");
                continue;
            }
            else if (key.Key == ConsoleKey.Backspace)
            {
                if (searchString.Length > 0)
                {
                    searchString = searchString[..^1];
                    Console.Write("\b \b");
                }
            }
            else if (key.KeyChar >= 32 && key.KeyChar <= 126) // Printable characters
            {
                searchString += key.KeyChar;
                Console.Write(key.KeyChar);
            }

            // Cancel previous search and start new one
            searchCancellationTokenSource.Cancel();
            searchCancellationTokenSource = new CancellationTokenSource();
            var token = searchCancellationTokenSource.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(searchDelayMs, token);
                    
                    if (string.IsNullOrWhiteSpace(searchString) || searchString.Length < minSearchLength)
                    {
                        currentResults.Clear();
                        Console.WriteLine($"\rSearch: {searchString} (Type at least {minSearchLength} characters)");
                        return;
                    }

                    var searchResponse = await httpClient.GetAsync($"api/choruses/search?q={Uri.EscapeDataString(searchString)}&searchIn=all&searchMode=Contains", token);
                    if (searchResponse.IsSuccessStatusCode)
                    {
                        var searchJson = await searchResponse.Content.ReadAsStringAsync(token);
                        var searchResult = JsonSerializer.Deserialize<JsonElement>(searchJson);
                        
                        if (searchResult.TryGetProperty("results", out var results))
                        {
                            currentResults = JsonSerializer.Deserialize<List<Chorus>>(results.GetRawText()) ?? new List<Chorus>();
                            
                            Console.Write($"\rSearch: {searchString} ({currentResults.Count} results)");
                            
                            if (currentResults.Count == 1)
                            {
                                Console.WriteLine("\n=== SINGLE CHORUS FOUND ===");
                                DisplayChorus(currentResults[0]);
                                Environment.Exit(0);
                            }
                            else if (currentResults.Count > 0)
                            {
                                Console.WriteLine();
                                for (int i = 0; i < Math.Min(currentResults.Count, 5); i++)
                                {
                                    var chorus = currentResults[i];
                                    Console.WriteLine($"  {i + 1}. {chorus.Name}");
                                }
                                if (currentResults.Count > 5)
                                {
                                    Console.WriteLine($"  ... and {currentResults.Count - 5} more");
                                }
                                Console.Write("Search: " + searchString);
                            }
                            else
                            {
                                Console.WriteLine(" (No results)");
                                Console.Write("Search: " + searchString);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Search was cancelled, ignore
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nSearch error: {ex.Message}");
                    Console.Write("Search: " + searchString);
                }
            }, token);
        }
    }

    private static async Task RunSlideConversionMode(string[] args, IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        // Get file path from args or config
        var filePath = args.Length > 1 ? args[1] : configuration["DefaultPpsxFilePath"];
        
        if (string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Please provide a file path as an argument or configure DefaultPpsxFilePath in appsettings.json");
            Console.WriteLine("Usage: dotnet run convert [filepath]");
            return;
        }

        // Resolve relative path
        var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Directory.GetCurrentDirectory(), filePath);
        
        if (!File.Exists(fullPath))
        {
            Console.WriteLine($"File not found: {fullPath}");
            return;
        }

        if (!fullPath.EndsWith(".ppsx", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Only .ppsx files are supported");
            return;
        }

        var debugApiCall = configuration.GetValue<bool>("DebugApiCall");

        Console.WriteLine($"Reading file: {fullPath}");
        
        // Read the file as binary
        var fileBytes = await File.ReadAllBytesAsync(fullPath);
        var fileName = Path.GetFileName(fullPath);
        
        if (debugApiCall)
        {
            Console.WriteLine($"File size: {fileBytes.Length} bytes");
            Console.WriteLine($"Filename: {fileName}");
        }

        // Call the API
        using var httpClient = httpClientFactory.CreateClient("CHAP2API");
        
        // Test if API is accessible first
        Console.WriteLine("Testing API connectivity...");
        try
        {
            var healthResponse = await httpClient.GetAsync("/api/health/ping");
            Console.WriteLine($"Health check status: {healthResponse.StatusCode}");
            if (healthResponse.IsSuccessStatusCode)
            {
                var healthContent = await healthResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Health check response: {healthContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Health check failed: {ex.Message}");
            Console.WriteLine($"API URL: {httpClient.BaseAddress}");
            Console.WriteLine("Make sure the API is running on http://localhost:5000");
            return;
        }
        
        Console.WriteLine("API is accessible, proceeding with file upload...");
        
        var request = new HttpRequestMessage(HttpMethod.Post, "api/slide/convert");
        request.Content = new ByteArrayContent(fileBytes);
        request.Headers.Add("X-Filename", fileName);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        // Always show debug info for troubleshooting
        Console.WriteLine("Calling API...");
        Console.WriteLine($"Content-Type: {request.Content.Headers.ContentType}");
        Console.WriteLine($"X-Filename: {request.Headers.GetValues("X-Filename").FirstOrDefault()}");
        Console.WriteLine($"Request URL: {httpClient.BaseAddress}api/slide/convert");
        Console.WriteLine($"File size being sent: {fileBytes.Length} bytes");
        
        var response = await httpClient.SendAsync(request);
        
        Console.WriteLine($"Response Status: {response.StatusCode}");
        
        string? chorusName = null;
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            if (debugApiCall)
            {
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                Console.WriteLine("Success! Response:");
                Console.WriteLine(JsonSerializer.Serialize(responseJson, new JsonSerializerOptions { WriteIndented = true }));
                if (responseJson.TryGetProperty("chorus", out var chorusObj))
                {
                    if (chorusObj.TryGetProperty("name", out var nameProp))
                        chorusName = nameProp.GetString();
                }
            }
            else
            {
                Console.WriteLine("Success!");
                // Try to parse chorusName anyway
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (responseJson.TryGetProperty("chorus", out var chorusObj))
                {
                    if (chorusObj.TryGetProperty("name", out var nameProp))
                        chorusName = nameProp.GetString();
                }
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error: {errorContent}");
        }

        // After conversion, offer to search for the created chorus
        if (!string.IsNullOrEmpty(chorusName))
        {
            Console.WriteLine($"\nChorus '{chorusName}' was created. Would you like to search for it? (y/n)");
            var key = Console.ReadKey();
            if (key.KeyChar == 'y' || key.KeyChar == 'Y')
            {
                Console.WriteLine("\nSearching for the created chorus...");
                var searchResponse = await httpClient.GetAsync($"api/choruses/search?q={Uri.EscapeDataString(chorusName)}&searchIn=name&searchMode=Exact");
                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchJson = await searchResponse.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<JsonElement>(searchJson);
                    
                    if (searchResult.TryGetProperty("results", out var results))
                    {
                        var choruses = JsonSerializer.Deserialize<List<Chorus>>(results.GetRawText()) ?? new List<Chorus>();
                        if (choruses.Count > 0)
                        {
                            Console.WriteLine("Found the created chorus:");
                            DisplayChorus(choruses[0]);
                        }
                    }
                }
            }
        }
    }

    private static void DisplayChorus(Chorus chorus)
    {
        Console.WriteLine($"  Id: {chorus.Id}");
        Console.WriteLine($"  Name: {chorus.Name}");
        Console.WriteLine($"  Key: {chorus.Key}");
        Console.WriteLine($"  TimeSignature: {chorus.TimeSignature}");
        Console.WriteLine($"  Type: {chorus.Type}");
        Console.WriteLine("  ChorusText:");
        Console.WriteLine(chorus.ChorusText);
    }
}
