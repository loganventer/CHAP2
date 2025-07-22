using System.Text.Json;
using CHAP2.Console.Vectorize.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Vectorize.Services;

public class ChorusDataService : IChorusDataService
{
    private readonly ILogger<ChorusDataService> _logger;

    public ChorusDataService(ILogger<ChorusDataService> logger)
    {
        _logger = logger;
    }

    public async Task<List<ChorusDataDto>> LoadChorusDataAsync(string dataPath)
    {
        var filePaths = await GetChorusFilePathsAsync(dataPath);
        var chorusData = new List<ChorusDataDto>();

        foreach (var filePath in filePaths)
        {
            try
            {
                var jsonContent = await File.ReadAllTextAsync(filePath);
                var chorus = JsonSerializer.Deserialize<ChorusDataDto>(jsonContent);
                
                if (chorus != null)
                {
                    chorusData.Add(chorus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading chorus data from {FilePath}", filePath);
            }
        }

        _logger.LogInformation("Loaded {Count} chorus records from {DataPath}", chorusData.Count, dataPath);
        return chorusData;
    }

    public async Task<List<string>> GetChorusFilePathsAsync(string dataPath)
    {
        if (!Directory.Exists(dataPath))
        {
            _logger.LogWarning("Data path {DataPath} does not exist", dataPath);
            return new List<string>();
        }

        var jsonFiles = Directory.GetFiles(dataPath, "*.json", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("Found {Count} JSON files in {DataPath}", jsonFiles.Length, dataPath);
        
        return jsonFiles.ToList();
    }
} 