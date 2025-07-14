using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CHAP2.Console.Common.Configuration;
using CHAP2.Console.Bulk.Services;

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
                
                // Register services
                services.AddScoped<IBulkUploadService, BulkUploadService>();
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        using var scope = host.Services.CreateScope();
        var bulkUploadService = scope.ServiceProvider.GetRequiredService<IBulkUploadService>();

        try
        {
            // Get path from args or config
            var path = args.Length > 0 ? args[0] : configuration["DefaultFolderPath"];
            
            if (string.IsNullOrWhiteSpace(path))
            {
                System.Console.WriteLine("CHAP2 Bulk Upload Console");
                System.Console.WriteLine("==========================");
                System.Console.WriteLine();
                System.Console.WriteLine("Usage: dotnet run [folder-path]");
                System.Console.WriteLine();
                System.Console.WriteLine("Examples:");
                System.Console.WriteLine("  dotnet run ./slides");
                System.Console.WriteLine("  dotnet run ../presentations");
                System.Console.WriteLine("  dotnet run C:\\MySlides");
                System.Console.WriteLine();
                System.Console.WriteLine("The application will recursively scan the specified folder");
                System.Console.WriteLine("for .ppsx and .pptx files and upload them to the CHAP2API.");
                return;
            }

            // Resolve path (handle home directory ~ and relative paths)
            var fullPath = ResolvePath(path);
            
            if (!Directory.Exists(fullPath))
            {
                System.Console.WriteLine($"Folder not found: {fullPath}");
                System.Console.WriteLine("Please provide a valid folder path.");
                return;
            }

            System.Console.WriteLine("CHAP2 Bulk Upload Console");
            System.Console.WriteLine("==========================");
            System.Console.WriteLine($"Target folder: {fullPath}");
            System.Console.WriteLine($"API Base URL: {configuration["ApiBaseUrl"]}");
            System.Console.WriteLine();

            // Perform bulk upload
            var successfulUploads = await bulkUploadService.UploadFolderAsync(fullPath);

            if (successfulUploads > 0)
            {
                System.Console.WriteLine($"✅ Bulk upload completed successfully! {successfulUploads} files uploaded.");
            }
            else
            {
                System.Console.WriteLine("❌ No files were uploaded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during bulk upload");
            System.Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static string ResolvePath(string path)
    {
        // Handle home directory expansion (~)
        if (path.StartsWith("~"))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var pathWithoutTilde = path.Substring(1);
            // Remove leading slash if present to avoid absolute path issues
            if (pathWithoutTilde.StartsWith("/"))
            {
                pathWithoutTilde = pathWithoutTilde.Substring(1);
            }
            var resolvedPath = Path.Combine(homeDir, pathWithoutTilde);
            System.Console.WriteLine($"Debug: Original path: '{path}'");
            System.Console.WriteLine($"Debug: Home directory: '{homeDir}'");
            System.Console.WriteLine($"Debug: Path without tilde: '{pathWithoutTilde}'");
            System.Console.WriteLine($"Debug: Resolved path: '{resolvedPath}'");
            return resolvedPath;
        }
        
        // Handle absolute paths
        if (Path.IsPathRooted(path))
        {
            return path;
        }
        
        // Handle relative paths
        return Path.Combine(Directory.GetCurrentDirectory(), path);
    }
}
