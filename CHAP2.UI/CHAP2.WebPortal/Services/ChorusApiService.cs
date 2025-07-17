using CHAP2.WebPortal.Interfaces;
using CHAP2.Domain.Entities;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CHAP2.WebPortal.Services;

public class ChorusApiService : IChorusApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChorusApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChorusApiService(IHttpClientFactory httpClientFactory, ILogger<ChorusApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CHAP2API");
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<bool> TestConnectivityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health/ping", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API connectivity");
            return false;
        }
    }

    public async Task<List<Chorus>> SearchChorusesAsync(string searchTerm, string searchMode = "Contains", string searchIn = "all", CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/api/choruses/search?q={Uri.EscapeDataString(searchTerm)}&searchIn={searchIn}&searchMode={searchMode}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResponse = JsonSerializer.Deserialize<SearchResponseDto>(content, _jsonOptions);
                
                if (searchResponse?.Results != null)
                {
                    var choruses = new List<Chorus>();
                    foreach (var dto in searchResponse.Results)
                    {
                        var chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                        
                        var chorusType = typeof(Chorus);
                        var idProperty = chorusType.GetProperty("Id");
                        if (idProperty != null)
                        {
                            idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                        }
                        
                        var keyProperty = chorusType.GetProperty("Key");
                        if (keyProperty != null)
                        {
                            keyProperty.SetValue(chorus, (CHAP2.Domain.Enums.MusicalKey)dto.Key);
                        }
                        
                        var typeProperty = chorusType.GetProperty("Type");
                        if (typeProperty != null)
                        {
                            typeProperty.SetValue(chorus, (CHAP2.Domain.Enums.ChorusType)dto.Type);
                        }
                        
                        var tsProperty = chorusType.GetProperty("TimeSignature");
                        if (tsProperty != null)
                        {
                            tsProperty.SetValue(chorus, (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature);
                        }
                        
                        choruses.Add(chorus);
                    }
                    
                    return choruses;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Search API failed: {Error}", errorContent);
            }

            return new List<Chorus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search choruses with term: {SearchTerm}", searchTerm);
            return new List<Chorus>();
        }
    }

    public async Task<Chorus?> GetChorusByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/choruses/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<ChorusDto>(content, _jsonOptions);
                
                if (dto != null)
                {
                    var chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                    
                    var chorusType = typeof(Chorus);
                    var idProperty = chorusType.GetProperty("Id");
                    if (idProperty != null)
                    {
                        idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                    }
                    
                    var keyProperty = chorusType.GetProperty("Key");
                    if (keyProperty != null)
                    {
                        keyProperty.SetValue(chorus, (CHAP2.Domain.Enums.MusicalKey)dto.Key);
                    }
                    
                    var typeProperty = chorusType.GetProperty("Type");
                    if (typeProperty != null)
                    {
                        typeProperty.SetValue(chorus, (CHAP2.Domain.Enums.ChorusType)dto.Type);
                    }
                    
                    var tsProperty = chorusType.GetProperty("TimeSignature");
                    if (tsProperty != null)
                    {
                        tsProperty.SetValue(chorus, (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature);
                    }
                    
                    return chorus;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chorus by ID: {Id}", id);
            return null;
        }
    }

    public async Task<Chorus?> GetChorusByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/choruses/by-name/{Uri.EscapeDataString(name)}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonSerializer.Deserialize<ChorusDto>(content, _jsonOptions);
                
                if (dto != null)
                {
                    var chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                    
                    var chorusType = typeof(Chorus);
                    var idProperty = chorusType.GetProperty("Id");
                    if (idProperty != null)
                    {
                        idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                    }
                    
                    var keyProperty = chorusType.GetProperty("Key");
                    if (keyProperty != null)
                    {
                        keyProperty.SetValue(chorus, (CHAP2.Domain.Enums.MusicalKey)dto.Key);
                    }
                    
                    var typeProperty = chorusType.GetProperty("Type");
                    if (typeProperty != null)
                    {
                        typeProperty.SetValue(chorus, (CHAP2.Domain.Enums.ChorusType)dto.Type);
                    }
                    
                    var tsProperty = chorusType.GetProperty("TimeSignature");
                    if (tsProperty != null)
                    {
                        tsProperty.SetValue(chorus, (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature);
                    }
                    
                    return chorus;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chorus by name: {Name}", name);
            return null;
        }
    }

    public async Task<List<Chorus>> GetAllChorusesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/choruses", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var dtos = JsonSerializer.Deserialize<List<ChorusDto>>(content, _jsonOptions);
                
                if (dtos != null)
                {
                    var choruses = new List<Chorus>();
                    foreach (var dto in dtos)
                    {
                        var chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                        
                        var chorusType = typeof(Chorus);
                        var idProperty = chorusType.GetProperty("Id");
                        if (idProperty != null)
                        {
                            idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                        }
                        
                        var keyProperty = chorusType.GetProperty("Key");
                        if (keyProperty != null)
                        {
                            keyProperty.SetValue(chorus, (CHAP2.Domain.Enums.MusicalKey)dto.Key);
                        }
                        
                        var typeProperty = chorusType.GetProperty("Type");
                        if (typeProperty != null)
                        {
                            typeProperty.SetValue(chorus, (CHAP2.Domain.Enums.ChorusType)dto.Type);
                        }
                        
                        var tsProperty = chorusType.GetProperty("TimeSignature");
                        if (tsProperty != null)
                        {
                            tsProperty.SetValue(chorus, (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature);
                        }
                        
                        choruses.Add(chorus);
                    }
                    
                    return choruses;
                }
            }

            return new List<Chorus>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all choruses");
            return new List<Chorus>();
        }
    }

    public async Task<Chorus?> ConvertSlideAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return null;
            }

            var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var fileName = Path.GetFileName(filePath);

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/slide/convert");
            request.Content = new ByteArrayContent(fileBytes);
            request.Headers.Add("X-Filename", fileName);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var slideResponse = JsonSerializer.Deserialize<SlideConversionResponseDto>(responseContent, _jsonOptions);
                
                if (slideResponse?.Chorus != null)
                {
                    var chorus = Chorus.CreateFromSlide(slideResponse.Chorus.Name, slideResponse.Chorus.ChorusText);
                    
                    var chorusType = typeof(Chorus);
                    var idProperty = chorusType.GetProperty("Id");
                    if (idProperty != null)
                    {
                        idProperty.SetValue(chorus, Guid.Parse(slideResponse.Chorus.Id));
                    }
                    
                    return chorus;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Slide conversion failed: {Error}", errorContent);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to convert slide: {FilePath}", filePath);
            return null;
        }
    }

    public async Task<bool> CreateChorusAsync(Chorus chorus, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = new ChorusDto
            {
                Id = chorus.Id.ToString(),
                Name = chorus.Name,
                ChorusText = chorus.ChorusText,
                Key = (int)chorus.Key,
                Type = (int)chorus.Type,
                TimeSignature = (int)chorus.TimeSignature
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/choruses", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chorus");
            return false;
        }
    }

    public async Task<bool> UpdateChorusAsync(Guid id, string name, string chorusText, CHAP2.Domain.Enums.MusicalKey key, CHAP2.Domain.Enums.ChorusType type, CHAP2.Domain.Enums.TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = new ChorusDto
            {
                Id = id.ToString(),
                Name = name,
                ChorusText = chorusText,
                Key = (int)key,
                Type = (int)type,
                TimeSignature = (int)timeSignature
            };

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"/api/choruses/{id}", content, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chorus with ID: {Id}", id);
            return false;
        }
    }

    public async Task<bool> DeleteChorusAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/choruses/{id}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chorus with ID: {Id}", id);
            return false;
        }
    }
}

// DTOs for API communication
public class SearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public string SearchMode { get; set; } = string.Empty;
    public string SearchIn { get; set; } = string.Empty;
    public int Count { get; set; }
    public int MaxResults { get; set; }
    public List<ChorusDto> Results { get; set; } = new();
}

public class ChorusDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ChorusText { get; set; } = string.Empty;
    public int Key { get; set; }
    public int Type { get; set; }
    public int TimeSignature { get; set; }
}

public class SlideConversionResponseDto
{
    public string Message { get; set; } = string.Empty;
    public ChorusDto Chorus { get; set; } = new();
    public string OriginalFilename { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
} 