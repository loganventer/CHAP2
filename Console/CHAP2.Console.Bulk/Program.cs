using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CHAP2.Common.Models;
using CHAP2.Console.Common.Configuration;

namespace CHAP2.Console.Bulk;

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
                services.AddHttpClient("CHAP2API", client =>
                {
                    client.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "http://localhost:5000");
                });
                
                // Configure settings
                services.Configure<BulkConversionSettings>(
                    configuration.GetSection("BulkConversionSettings"));
                services.Configure<ApiClientSettings>(
                    configuration.GetSection("ApiClientSettings"));
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
                System.Console.WriteLine("Please provide a file path as an argument or configure DefaultPpsxFilePath in appsettings.json");
                System.Console.WriteLine("Usage: dotnet run [filepath]");
                return;
            }

            // Resolve relative path
            var fullPath = Path.IsPathRooted(filePath) ? filePath : Path.Combine(Directory.GetCurrentDirectory(), filePath);
            
            if (!File.Exists(fullPath))
            {
                System.Console.WriteLine($"File not found: {fullPath}");
                return;
            }

            if (!fullPath.EndsWith(".ppsx", StringComparison.OrdinalIgnoreCase))
            {
                System.Console.WriteLine("Only .ppsx files are supported");
                return;
            }

            var debugApiCall = configuration.GetValue<bool>("DebugApiCall");

            System.Console.WriteLine($"Reading file: {fullPath}");
            
            // Read the file as binary
            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var fileName = Path.GetFileName(fullPath);
            
            if (debugApiCall)
            {
                System.Console.WriteLine($"File size: {fileBytes.Length} bytes");
                System.Console.WriteLine($"Filename: {fileName}");
            }

            // Call the API
            using var httpClient = httpClientFactory.CreateClient("CHAP2API");
            
            // Test if API is accessible first
            System.Console.WriteLine("Testing API connectivity...");
            try
            {
                var healthResponse = await httpClient.GetAsync("/api/health/ping");
                if (!healthResponse.IsSuccessStatusCode)
                {
                    System.Console.WriteLine("API is not accessible. Make sure the API is running.");
                    return;
                }
                System.Console.WriteLine("API is accessible.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error connecting to API: {ex.Message}");
                return;
            }

            System.Console.WriteLine("Calling API...");
            
            // Create the request
            var request = new HttpRequestMessage(HttpMethod.Post, "api/slide/convert");
            request.Content = new ByteArrayContent(fileBytes);
            request.Headers.Add("X-Filename", fileName);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.SendAsync(request);

            System.Console.WriteLine($"Response Status: {(int)response.StatusCode} {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine("Success! Response:");
                System.Console.WriteLine(responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
