using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CHAP2.Console.Common.Configuration;
using CHAP2.Common.Models;
using System.Text.Json;

namespace CHAP2.Console.Bulk.Services;

public interface IBulkUploadService
{
    Task<int> UploadFolderAsync(string folderPath, CancellationToken cancellationToken = default);
    Task<UploadResult> UploadFileAsync(string filePath, CancellationToken cancellationToken = default);
}

public class UploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Chorus? Chorus { get; set; }
}

public class BulkUploadService : IBulkUploadService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BulkUploadService> _logger;
    private readonly ApiClientSettings _apiSettings;

    public BulkUploadService(
        IHttpClientFactory httpClientFactory,
        ILogger<BulkUploadService> logger,
        IOptions<ApiClientSettings> apiSettings)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiSettings = apiSettings.Value;
    }

    public async Task<int> UploadFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(folderPath))
        {
            _logger.LogError("Folder not found: {FolderPath}", folderPath);
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        System.Console.WriteLine($"Scanning folder: {folderPath}");
        
        // Find all .ppsx files recursively
        var ppsxFiles = Directory.GetFiles(folderPath, "*.ppsx", SearchOption.AllDirectories);
        var pptxFiles = Directory.GetFiles(folderPath, "*.pptx", SearchOption.AllDirectories);
        var allFiles = ppsxFiles.Concat(pptxFiles).ToList();

        System.Console.WriteLine($"Found {allFiles.Count} PowerPoint files:");
        foreach (var file in allFiles)
        {
            System.Console.WriteLine($"  {file}");
        }
        System.Console.WriteLine();

        if (allFiles.Count == 0)
        {
            System.Console.WriteLine("No PowerPoint files found in the specified folder.");
            return 0;
        }

        // Test API connectivity
        System.Console.WriteLine("Testing API connectivity...");
        using var httpClient = _httpClientFactory.CreateClient("CHAP2API");
        try
        {
            var healthResponse = await httpClient.GetAsync("/api/health/ping", cancellationToken);
            if (!healthResponse.IsSuccessStatusCode)
            {
                System.Console.WriteLine("API is not accessible. Make sure the API is running.");
                return 0;
            }
            System.Console.WriteLine("API is accessible.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error connecting to API: {ex.Message}");
            return 0;
        }

        System.Console.WriteLine($"Starting bulk upload of {allFiles.Count} files...");
        System.Console.WriteLine();

        var successfulUploads = 0;
        var failedUploads = 0;

        for (int i = 0; i < allFiles.Count; i++)
        {
            var file = allFiles[i];
            System.Console.WriteLine($"[{i + 1}/{allFiles.Count}] Processing: {Path.GetFileName(file)}");
            
            try
            {
                var result = await UploadFileAsync(file, cancellationToken);
                if (result.Success)
                {
                    successfulUploads++;
                    System.Console.WriteLine($"  ✅ Success: {result.Chorus?.Name ?? "Unknown chorus"}");
                }
                else
                {
                    failedUploads++;
                    System.Console.WriteLine($"  ❌ Failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                failedUploads++;
                System.Console.WriteLine($"  ❌ Error: {ex.Message}");
            }

            System.Console.WriteLine();
        }

        System.Console.WriteLine("=== BULK UPLOAD SUMMARY ===");
        System.Console.WriteLine($"Total files: {allFiles.Count}");
        System.Console.WriteLine($"Successful: {successfulUploads}");
        System.Console.WriteLine($"Failed: {failedUploads}");
        System.Console.WriteLine($"Success rate: {(double)successfulUploads / allFiles.Count * 100:F1}%");

        return successfulUploads;
    }

    public async Task<UploadResult> UploadFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var result = new UploadResult
        {
            FileName = Path.GetFileName(filePath)
        };

        try
        {
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = "File not found";
                return result;
            }

            // Read the file as binary
            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var fileName = Path.GetFileName(filePath);

            // Call the API
            using var httpClient = _httpClientFactory.CreateClient("CHAP2API");
            
            // Create the request
            var request = new HttpRequestMessage(HttpMethod.Post, "api/slide/convert");
            request.Content = new ByteArrayContent(fileBytes);
            request.Headers.Add("X-Filename", fileName);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    result.Chorus = JsonSerializer.Deserialize<Chorus>(responseContent, options);
                    result.Success = true;
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = $"Failed to parse response: {ex.Message}";
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {errorContent}";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
} 