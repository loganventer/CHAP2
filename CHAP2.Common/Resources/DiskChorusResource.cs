using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Linq;
using CHAP2.Common.Models;
using CHAP2.Common.Interfaces;

namespace CHAP2.Common.Resources;

public class DiskChorusResource : IChorusResource
{
    private readonly string _folderPath;
    public DiskChorusResource(ChorusResourceOptions options)
    {
        _folderPath = Path.IsPathRooted(options.FolderPath)
            ? options.FolderPath
            : Path.Combine(Directory.GetCurrentDirectory(), options.FolderPath);
        Directory.CreateDirectory(_folderPath);
    }

    public async Task AddChorusAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chorus);
        
        // Use GUID for filename to ensure uniqueness and handle name changes
        var fileName = Path.Combine(_folderPath, $"{chorus.Id}.json");
        var json = JsonSerializer.Serialize(chorus, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileName, json, cancellationToken);
    }

    public async Task<IReadOnlyList<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<Chorus>();
        foreach (var file in Directory.GetFiles(_folderPath, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var chorus = JsonSerializer.Deserialize<Chorus>(json);
            if (chorus != null)
                result.Add(chorus);
        }
        return result;
    }

    public async Task UpdateChorusAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chorus);
        
        var fileName = Path.Combine(_folderPath, $"{chorus.Id}.json");
        var json = JsonSerializer.Serialize(chorus, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileName, json, cancellationToken);
    }

    public async Task<Chorus?> GetChorusByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileName = Path.Combine(_folderPath, $"{id}.json");
        if (!File.Exists(fileName))
            return null;
            
        var json = await File.ReadAllTextAsync(fileName, cancellationToken);
        return JsonSerializer.Deserialize<Chorus>(json);
    }

    public async Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var allChoruses = await GetAllChorusesAsync(cancellationToken);
        return allChoruses.FirstOrDefault(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> ChorusExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        var allChoruses = await GetAllChorusesAsync(cancellationToken);
        return allChoruses.Any(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }
} 