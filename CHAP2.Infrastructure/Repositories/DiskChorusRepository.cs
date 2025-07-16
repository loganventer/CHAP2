using CHAP2.Application.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using CHAP2.Infrastructure.DTOs;

namespace CHAP2.Infrastructure.Repositories;

public class DiskChorusRepository : IChorusRepository
{
    private readonly string _folderPath;
    private readonly ILogger<DiskChorusRepository> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public DiskChorusRepository(string folderPath, ILogger<DiskChorusRepository> logger)
    {
        _folderPath = Path.IsPathRooted(folderPath)
            ? folderPath
            : Path.Combine(Directory.GetCurrentDirectory(), folderPath);
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            Converters = { 
                new CHAP2.Domain.ValueObjects.ChorusMetadataJsonConverter()
            }
        };
        
        Directory.CreateDirectory(_folderPath);
    }

    public async Task<Chorus?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileName = Path.Combine(_folderPath, $"{id}.json");
        if (!File.Exists(fileName))
        {
            _logger.LogDebug("Chorus with ID {ChorusId} not found", id);
            return null;
        }
            
        try
        {
            var json = await File.ReadAllTextAsync(fileName, cancellationToken);
            var chorusDto = JsonSerializer.Deserialize<ChorusDto>(json, _jsonOptions);
            var chorus = chorusDto?.ToEntity();
            
            if (chorus != null)
            {
                _logger.LogDebug("Retrieved chorus with ID {ChorusId} and name '{ChorusName}'", id, chorus.Name);
            }
            
            return chorus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading chorus with ID {ChorusId} from file {FileName}", id, fileName);
            return null;
        }
    }

    public async Task<Chorus?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var allChoruses = await GetAllAsync(cancellationToken);
        var chorus = allChoruses.FirstOrDefault(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            
        if (chorus != null)
        {
            _logger.LogDebug("Found chorus with name '{ChorusName}' and ID {ChorusId}", name, chorus.Id);
        }
        else
        {
            _logger.LogDebug("No chorus found with name '{ChorusName}'", name);
        }
        
        return chorus;
    }

    public async Task<IReadOnlyList<Chorus>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = Directory.GetFiles(_folderPath, "*.json");
            if (files.Length == 0)
            {
                return new List<Chorus>();
            }

            var tasks = files.Select(async file =>
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file, cancellationToken);
                    var chorusDto = JsonSerializer.Deserialize<ChorusDto>(json, _jsonOptions);
                    return chorusDto?.ToEntity();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize chorus from file {FileName}", file);
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);
            var validChoruses = results.OfType<Chorus>().ToList();
            
            _logger.LogInformation("Loaded {Count} choruses from {FileCount} files", validChoruses.Count, files.Length);
            return validChoruses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading choruses from directory {FolderPath}", _folderPath);
            throw;
        }
    }

    public async Task<IReadOnlyList<Chorus>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);
        
        var tasks = ids.Select(id => GetByIdAsync(id, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.OfType<Chorus>().ToList();
    }

    public async Task<Chorus> AddAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chorus);
        
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var fileName = Path.Combine(_folderPath, $"{chorus.Id}.json");
            var chorusDto = ChorusDto.FromEntity(chorus);
            var json = JsonSerializer.Serialize(chorusDto, _jsonOptions);
            await File.WriteAllTextAsync(fileName, json, cancellationToken);
            
            _logger.LogInformation("Added chorus with ID {ChorusId} and name '{ChorusName}'", chorus.Id, chorus.Name);
            return chorus;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<Chorus> UpdateAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chorus);
        
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            var fileName = Path.Combine(_folderPath, $"{chorus.Id}.json");
            var chorusDto = ChorusDto.FromEntity(chorus);
            var json = JsonSerializer.Serialize(chorusDto, _jsonOptions);
            await File.WriteAllTextAsync(fileName, json, cancellationToken);
            
            _logger.LogInformation("Updated chorus with ID {ChorusId} and name '{ChorusName}'", chorus.Id, chorus.Name);
            return chorus;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileName = Path.Combine(_folderPath, $"{id}.json");
        if (!File.Exists(fileName))
        {
            _logger.LogWarning("Attempted to delete non-existent chorus with ID {ChorusId}", id);
            return;
        }

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            File.Delete(fileName);
            _logger.LogInformation("Deleted chorus with ID {ChorusId}", id);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        var allChoruses = await GetAllAsync(cancellationToken);
        var exists = allChoruses.Any(c => 
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            
        _logger.LogDebug("Chorus with name '{ChorusName}' exists: {Exists}", name, exists);
        return exists;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileName = Path.Combine(_folderPath, $"{id}.json");
        return Task.FromResult(File.Exists(fileName));
    }

    public async Task<IReadOnlyList<Chorus>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);
        
        var all = await GetAllAsync(cancellationToken);
        var normalizedSearchTerm = searchTerm.ToLowerInvariant();
        
        var scoredResults = all.Select(c => new
        {
            Chorus = c,
            Score = CalculateSearchScore(c, normalizedSearchTerm)
        })
        .Where(result => result.Score > 0)
        .OrderByDescending(result => result.Score)
        .ThenBy(result => result.Chorus.Name)
        .Select(result => result.Chorus)
        .ToList();
        
        return scoredResults;
    }

    private static int CalculateSearchScore(Chorus chorus, string searchTerm)
    {
        var score = 0;
        
        // Key match gets highest priority
        if (MatchesMusicalKey(chorus.Key, searchTerm))
        {
            // Exact key match gets highest priority (score 200)
            if (IsExactKeyMatch(chorus.Key, searchTerm))
            {
                score += 200;
            }
            // Other key variations get high priority (score 100)
            else
            {
                score += 100;
            }
        }
        
        // Title match gets medium priority (score 50)
        if (chorus.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 50;
            
            // Bonus for exact title match (score +25)
            if (chorus.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                score += 25;
            }
            
            // Bonus for title starting with search term (score +10)
            if (chorus.Name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }
        }
        
        // Text match gets lowest priority (score 10)
        if (chorus.ChorusText.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }
        
        return score;
    }

    private static bool IsExactKeyMatch(MusicalKey key, string searchTerm)
    {
        if (key == MusicalKey.NotSet) return false;
        
        // Check for exact matches
        var keyName = key.ToString().ToLowerInvariant();
        if (keyName == searchTerm) return true;
        
        // Check for exact matches in variations
        var variations = GetKeyVariations(key);
        return variations.Any(variation => variation == searchTerm);
    }

    private static bool MatchesMusicalKey(MusicalKey key, string searchTerm)
    {
        if (key == MusicalKey.NotSet) return false;
        
        // Get the key name and normalize it
        var keyName = key.ToString().ToLowerInvariant();
        
        // Direct match
        if (keyName.Contains(searchTerm)) return true;
        
        // Handle common musical key variations
        var keyVariations = GetKeyVariations(key);
        return keyVariations.Any(variation => variation.Contains(searchTerm));
    }

    private static IEnumerable<string> GetKeyVariations(MusicalKey key)
    {
        var variations = new List<string>();
        
        switch (key)
        {
            case MusicalKey.CSharp:
                variations.AddRange(new[] { "c#", "c sharp", "cis", "c-sharp" });
                break;
            case MusicalKey.DSharp:
                variations.AddRange(new[] { "d#", "d sharp", "dis", "d-sharp" });
                break;
            case MusicalKey.FSharp:
                variations.AddRange(new[] { "f#", "f sharp", "fis", "f-sharp" });
                break;
            case MusicalKey.GSharp:
                variations.AddRange(new[] { "g#", "g sharp", "gis", "g-sharp" });
                break;
            case MusicalKey.ASharp:
                variations.AddRange(new[] { "a#", "a sharp", "ais", "a-sharp" });
                break;
            case MusicalKey.CFlat:
                variations.AddRange(new[] { "cb", "c flat", "ces", "c-flat" });
                break;
            case MusicalKey.DFlat:
                variations.AddRange(new[] { "db", "d flat", "des", "d-flat" });
                break;
            case MusicalKey.EFlat:
                variations.AddRange(new[] { "eb", "e flat", "es", "e-flat" });
                break;
            case MusicalKey.FFlat:
                variations.AddRange(new[] { "fb", "f flat", "fes", "f-flat" });
                break;
            case MusicalKey.GFlat:
                variations.AddRange(new[] { "gb", "g flat", "ges", "g-flat" });
                break;
            case MusicalKey.AFlat:
                variations.AddRange(new[] { "ab", "a flat", "as", "a-flat" });
                break;
            case MusicalKey.BFlat:
                variations.AddRange(new[] { "bb", "b flat", "bes", "b-flat" });
                break;
            default:
                // For natural keys (C, D, E, F, G, A, B), just add the basic name
                variations.Add(key.ToString().ToLowerInvariant());
                break;
        }
        
        return variations;
    }

    public async Task<IReadOnlyList<Chorus>> GetByKeyAsync(MusicalKey key, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(c => c.Key == key).ToList();
    }

    public async Task<IReadOnlyList<Chorus>> GetByTimeSignatureAsync(TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.Where(c => c.TimeSignature == timeSignature).ToList();
    }
} 