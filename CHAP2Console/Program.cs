using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CHAP2Console;

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
            // Get file path from args or config
            var filePath = args.Length > 0 ? args[0] : configuration["DefaultPpsxFilePath"];
            
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("Please provide a file path as an argument or configure DefaultPpsxFilePath in appsettings.json");
                Console.WriteLine("Usage: dotnet run [filepath]");
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
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                if (debugApiCall)
                {
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    Console.WriteLine("Success! Response:");
                    Console.WriteLine(JsonSerializer.Serialize(responseJson, new JsonSerializerOptions { WriteIndented = true }));
                }
                else
                {
                    Console.WriteLine("Success!");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
