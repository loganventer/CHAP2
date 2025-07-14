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

    public async Task AddChorusAsync(Chorus chorus)
    {
        // Create a safe filename from the chorus name
        var safeName = string.Join("", chorus.Name.Split(' ', '-', '_', '.', '/', '\\')
            .Where(w => !string.IsNullOrEmpty(w))
            .Select((w, i) => i == 0 ? w.ToLowerInvariant() : char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant()));
        
        var fileName = Path.Combine(_folderPath, $"{safeName}.json");
        var json = JsonSerializer.Serialize(chorus, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(fileName, json);
    }

    public async Task<IReadOnlyList<Chorus>> GetAllChorusesAsync()
    {
        var result = new List<Chorus>();
        foreach (var file in Directory.GetFiles(_folderPath, "*.json"))
        {
            var json = await File.ReadAllTextAsync(file);
            var chorus = JsonSerializer.Deserialize<Chorus>(json);
            if (chorus != null)
                result.Add(chorus);
        }
        return result;
    }
} 