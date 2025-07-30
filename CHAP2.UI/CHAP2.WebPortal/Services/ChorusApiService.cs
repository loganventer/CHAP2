using CHAP2.WebPortal.Interfaces;
using CHAP2.Domain.Entities;
using CHAP2.Shared.DTOs;
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
            _logger.LogInformation("=== API CONNECTIVITY TEST START ===");
            _logger.LogInformation("HTTP Client Base Address: {BaseAddress}", _httpClient.BaseAddress);
            
            var response = await _httpClient.GetAsync("/api/health/ping", cancellationToken);
            _logger.LogInformation("Connectivity test response status: {StatusCode}", response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Connectivity test response content: {Content}", content);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Connectivity test failed: {Error}", errorContent);
            }
            
            _logger.LogInformation("=== API CONNECTIVITY TEST END - SUCCESS: {Success} ===", response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test API connectivity. Exception: {ExceptionType}", ex.GetType().Name);
            return false;
        }
    }

    public async Task<List<Chorus>> SearchChorusesAsync(string searchTerm, string searchMode = "Contains", string searchIn = "all", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Searching choruses with term: {SearchTerm}, mode: {SearchMode}, in: {SearchIn}", searchTerm, searchMode, searchIn);
            
            var url = $"/api/choruses/search?q={Uri.EscapeDataString(searchTerm)}&searchIn={searchIn}&searchMode={searchMode}";
            _logger.LogInformation("Search URL: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            _logger.LogInformation("Search API response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Search API response content: {Content}", content);
                
                var searchResponse = JsonSerializer.Deserialize<ApiSearchResponseDto>(content, _jsonOptions);
                
                if (searchResponse?.Results != null)
                {
                    _logger.LogInformation("Found {Count} search results", searchResponse.Results.Count);
                    
                    var choruses = new List<Chorus>();
                    foreach (var dto in searchResponse.Results)
                    {
                        _logger.LogInformation("Processing search result - Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                            dto.Name, dto.Key, dto.Type, dto.TimeSignature);
                        
                        try
                        {
                            // Convert enum values directly without changing NotSet
                            var key = (CHAP2.Domain.Enums.MusicalKey)dto.Key;
                            var type = (CHAP2.Domain.Enums.ChorusType)dto.Type;
                            var timeSignature = (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature;
                            
                            // Use CreateFromSlide for NotSet values, Create for set values
                            Chorus chorus;
                            if (key == CHAP2.Domain.Enums.MusicalKey.NotSet || 
                                type == CHAP2.Domain.Enums.ChorusType.NotSet || 
                                timeSignature == CHAP2.Domain.Enums.TimeSignature.NotSet)
                            {
                                chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                                
                                // Set the enum values via reflection if they're not NotSet
                                var chorusType = typeof(Chorus);
                                if (key != CHAP2.Domain.Enums.MusicalKey.NotSet)
                                {
                                    var keyProperty = chorusType.GetProperty("Key");
                                    keyProperty?.SetValue(chorus, key);
                                }
                                if (type != CHAP2.Domain.Enums.ChorusType.NotSet)
                                {
                                    var typeProperty = chorusType.GetProperty("Type");
                                    typeProperty?.SetValue(chorus, type);
                                }
                                if (timeSignature != CHAP2.Domain.Enums.TimeSignature.NotSet)
                                {
                                    var timeSignatureProperty = chorusType.GetProperty("TimeSignature");
                                    timeSignatureProperty?.SetValue(chorus, timeSignature);
                                }
                            }
                            else
                            {
                                chorus = Chorus.Create(dto.Name, dto.ChorusText, key, type, timeSignature);
                            }
                            
                            // Set the ID using reflection since it's private set
                            var chorusTypeForId = typeof(Chorus);
                            var idProperty = chorusTypeForId.GetProperty("Id");
                            if (idProperty != null)
                            {
                                idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                            }
                            
                            _logger.LogInformation("Successfully created chorus from search result: {Name}", chorus.Name);
                            choruses.Add(chorus);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error creating chorus from search result - Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                                dto.Name, dto.Key, dto.Type, dto.TimeSignature);
                        }
                    }
                    
                    _logger.LogInformation("Returning {Count} choruses from search", choruses.Count);
                    return choruses;
                }
                else
                {
                    _logger.LogWarning("Search response or results is null");
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
            _logger.LogInformation("Getting chorus by ID: {Id}", id);
            
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("Empty ID provided to GetChorusByIdAsync");
                return null;
            }
            
            var response = await _httpClient.GetAsync($"/api/choruses/{id}", cancellationToken);
            _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("API response content: {Content}", content);
                
                var dto = JsonSerializer.Deserialize<ApiChorusDto>(content, _jsonOptions);
                
                if (dto != null)
                {
                    _logger.LogInformation("Creating chorus from DTO - Name: {Name}, Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                        dto.Name, dto.Key, dto.Type, dto.TimeSignature);
                    
                    try
                    {
                        // Convert enum values directly
                        var key = (CHAP2.Domain.Enums.MusicalKey)dto.Key;
                        var type = (CHAP2.Domain.Enums.ChorusType)dto.Type;
                        var timeSignature = (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature;
                        
                        // Use CreateFromSlide for NotSet values, Create for set values
                        Chorus chorus;
                        if (key == CHAP2.Domain.Enums.MusicalKey.NotSet || 
                            type == CHAP2.Domain.Enums.ChorusType.NotSet || 
                            timeSignature == CHAP2.Domain.Enums.TimeSignature.NotSet)
                        {
                            chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                            
                            // Set the enum values via reflection if they're not NotSet
                            var chorusType = typeof(Chorus);
                            if (key != CHAP2.Domain.Enums.MusicalKey.NotSet)
                            {
                                var keyProperty = chorusType.GetProperty("Key");
                                keyProperty?.SetValue(chorus, key);
                            }
                            if (type != CHAP2.Domain.Enums.ChorusType.NotSet)
                            {
                                var typeProperty = chorusType.GetProperty("Type");
                                typeProperty?.SetValue(chorus, type);
                            }
                            if (timeSignature != CHAP2.Domain.Enums.TimeSignature.NotSet)
                            {
                                var timeSignatureProperty = chorusType.GetProperty("TimeSignature");
                                timeSignatureProperty?.SetValue(chorus, timeSignature);
                            }
                        }
                        else
                        {
                            chorus = Chorus.Create(dto.Name, dto.ChorusText, key, type, timeSignature);
                        }
                        
                        // Set the ID using reflection since it's private set
                        var chorusTypeForId = typeof(Chorus);
                        var idProperty = chorusTypeForId.GetProperty("Id");
                        if (idProperty != null)
                        {
                            idProperty.SetValue(chorus, Guid.Parse(dto.Id));
                        }
                        
                        _logger.LogInformation("Chorus loaded successfully: {Name}", chorus.Name);
                        return chorus;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating chorus from DTO");
                        return null;
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("API error response: {Error}", errorContent);
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
                var dto = JsonSerializer.Deserialize<ApiChorusDto>(content, _jsonOptions);
                
                if (dto != null)
                {
                    // Convert enum values directly
                    var key = (CHAP2.Domain.Enums.MusicalKey)dto.Key;
                    var type = (CHAP2.Domain.Enums.ChorusType)dto.Type;
                    var timeSignature = (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature;
                    
                    // Use CreateFromSlide for NotSet values, Create for set values
                    Chorus chorus;
                    if (key == CHAP2.Domain.Enums.MusicalKey.NotSet || 
                        type == CHAP2.Domain.Enums.ChorusType.NotSet || 
                        timeSignature == CHAP2.Domain.Enums.TimeSignature.NotSet)
                    {
                        chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                        
                        // Set the enum values via reflection if they're not NotSet
                        var chorusType = typeof(Chorus);
                        if (key != CHAP2.Domain.Enums.MusicalKey.NotSet)
                        {
                            var keyProperty = chorusType.GetProperty("Key");
                            keyProperty?.SetValue(chorus, key);
                        }
                        if (type != CHAP2.Domain.Enums.ChorusType.NotSet)
                        {
                            var typeProperty = chorusType.GetProperty("Type");
                            typeProperty?.SetValue(chorus, type);
                        }
                        if (timeSignature != CHAP2.Domain.Enums.TimeSignature.NotSet)
                        {
                            var timeSignatureProperty = chorusType.GetProperty("TimeSignature");
                            timeSignatureProperty?.SetValue(chorus, timeSignature);
                        }
                    }
                    else
                    {
                        chorus = Chorus.Create(dto.Name, dto.ChorusText, key, type, timeSignature);
                    }
                    
                    // Set the ID using reflection since it's private set
                    var chorusTypeForId = typeof(Chorus);
                    var idProperty = chorusTypeForId.GetProperty("Id");
                    if (idProperty != null)
                    {
                        idProperty.SetValue(chorus, Guid.Parse(dto.Id));
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
                var dtos = JsonSerializer.Deserialize<List<ApiChorusDto>>(content, _jsonOptions);
                
                if (dtos != null)
                {
                    var choruses = new List<Chorus>();
                    foreach (var dto in dtos)
                    {
                        // Convert enum values directly
                        var key = (CHAP2.Domain.Enums.MusicalKey)dto.Key;
                        var type = (CHAP2.Domain.Enums.ChorusType)dto.Type;
                        var timeSignature = (CHAP2.Domain.Enums.TimeSignature)dto.TimeSignature;
                        
                        // Use CreateFromSlide for NotSet values, Create for set values
                        Chorus chorus;
                        if (key == CHAP2.Domain.Enums.MusicalKey.NotSet || 
                            type == CHAP2.Domain.Enums.ChorusType.NotSet || 
                            timeSignature == CHAP2.Domain.Enums.TimeSignature.NotSet)
                        {
                            chorus = Chorus.CreateFromSlide(dto.Name, dto.ChorusText);
                            
                            // Set the enum values via reflection if they're not NotSet
                            var chorusType = typeof(Chorus);
                            if (key != CHAP2.Domain.Enums.MusicalKey.NotSet)
                            {
                                var keyProperty = chorusType.GetProperty("Key");
                                keyProperty?.SetValue(chorus, key);
                            }
                            if (type != CHAP2.Domain.Enums.ChorusType.NotSet)
                            {
                                var typeProperty = chorusType.GetProperty("Type");
                                typeProperty?.SetValue(chorus, type);
                            }
                            if (timeSignature != CHAP2.Domain.Enums.TimeSignature.NotSet)
                            {
                                var timeSignatureProperty = chorusType.GetProperty("TimeSignature");
                                timeSignatureProperty?.SetValue(chorus, timeSignature);
                            }
                        }
                        else
                        {
                            chorus = Chorus.Create(dto.Name, dto.ChorusText, key, type, timeSignature);
                        }
                        
                        // Set the ID using reflection since it's private set
                        var chorusTypeForId = typeof(Chorus);
                        var idProperty = chorusTypeForId.GetProperty("Id");
                        if (idProperty != null)
                        {
                            idProperty.SetValue(chorus, Guid.Parse(dto.Id));
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
                var slideResponse = JsonSerializer.Deserialize<ApiSlideConversionResponseDto>(responseContent, _jsonOptions);
                
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
            _logger.LogInformation("=== CREATE CHORUS API CALL START ===");
            _logger.LogInformation("Creating chorus via API: {Name} (ID: {Id})", chorus.Name, chorus.Id);
            _logger.LogInformation("Chorus details - Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", 
                chorus.Key, chorus.Type, chorus.TimeSignature);
            _logger.LogInformation("HTTP Client Base Address: {BaseAddress}", _httpClient.BaseAddress);
            
            var dto = new ApiChorusDto
            {
                Id = chorus.Id.ToString(),
                Name = chorus.Name,
                ChorusText = chorus.ChorusText,
                Key = (int)chorus.Key,
                Type = (int)chorus.Type,
                TimeSignature = (int)chorus.TimeSignature
            };

            _logger.LogInformation("DTO created - ID: {DtoId}, Name: {DtoName}, Key: {DtoKey}, Type: {DtoType}, TimeSignature: {DtoTimeSignature}", 
                dto.Id, dto.Name, dto.Key, dto.Type, dto.TimeSignature);

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            _logger.LogInformation("JSON serialized: {Json}", json);
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            _logger.LogInformation("Content created with Content-Type: {ContentType}", content.Headers.ContentType);
            
            var url = "/api/choruses";
            var fullUrl = $"{_httpClient.BaseAddress}{url}";
            _logger.LogInformation("Making POST request to: {FullUrl}", fullUrl);
            
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("API error response: {Error}", errorContent);
                var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                _logger.LogError("Response headers: {Headers}", headers);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("API success response: {Response}", responseContent);
            }
            
            _logger.LogInformation("=== CREATE CHORUS API CALL END - SUCCESS: {Success} ===", response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chorus. Exception: {ExceptionType}", ex.GetType().Name);
            return false;
        }
    }

    public async Task<bool> UpdateChorusAsync(Guid id, string name, string chorusText, CHAP2.Domain.Enums.MusicalKey key, CHAP2.Domain.Enums.ChorusType type, CHAP2.Domain.Enums.TimeSignature timeSignature, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== UPDATE CHORUS API CALL START ===");
            _logger.LogInformation("Updating chorus via API - ID: {Id}, Name: {Name}", id, name);
            _logger.LogInformation("Update parameters - Key: {Key}, Type: {Type}, TimeSignature: {TimeSignature}", key, type, timeSignature);
            _logger.LogInformation("HTTP Client Base Address: {BaseAddress}", _httpClient.BaseAddress);
            
            var dto = new ApiChorusDto
            {
                Id = id.ToString(),
                Name = name,
                ChorusText = chorusText,
                Key = (int)key,
                Type = (int)type,
                TimeSignature = (int)timeSignature
            };

            _logger.LogInformation("DTO created - ID: {DtoId}, Name: {DtoName}, Key: {DtoKey}, Type: {DtoType}, TimeSignature: {DtoTimeSignature}", 
                dto.Id, dto.Name, dto.Key, dto.Type, dto.TimeSignature);

            var json = JsonSerializer.Serialize(dto, _jsonOptions);
            _logger.LogInformation("JSON serialized: {Json}", json);
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            _logger.LogInformation("Content created with Content-Type: {ContentType}", content.Headers.ContentType);
            
            var url = $"/api/choruses/{id}";
            var fullUrl = $"{_httpClient.BaseAddress}{url}";
            _logger.LogInformation("Making PUT request to: {FullUrl}", fullUrl);
            
            var response = await _httpClient.PutAsync(url, content, cancellationToken);
            _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("API error response: {Error}", errorContent);
                var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                _logger.LogError("Response headers: {Headers}", headers);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("API success response: {Response}", responseContent);
            }
            
            _logger.LogInformation("=== UPDATE CHORUS API CALL END - SUCCESS: {Success} ===", response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update chorus with ID: {Id}. Exception: {ExceptionType}", id, ex.GetType().Name);
            return false;
        }
    }

    public async Task<bool> DeleteChorusAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== DELETE CHORUS API CALL START ===");
            _logger.LogInformation("Deleting chorus via API - ID: {Id}", id);
            _logger.LogInformation("HTTP Client Base Address: {BaseAddress}", _httpClient.BaseAddress);
            
            var url = $"/api/choruses/{id}";
            var fullUrl = $"{_httpClient.BaseAddress}{url}";
            _logger.LogInformation("Making DELETE request to: {FullUrl}", fullUrl);
            
            var response = await _httpClient.DeleteAsync(url, cancellationToken);
            _logger.LogInformation("API response status: {StatusCode}", response.StatusCode);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("API error response: {Error}", errorContent);
                var headers = string.Join(", ", response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}"));
                _logger.LogError("Response headers: {Headers}", headers);
            }
            else
            {
                _logger.LogInformation("Chorus deleted successfully via API");
            }
            
            _logger.LogInformation("=== DELETE CHORUS API CALL END - SUCCESS: {Success} ===", response.IsSuccessStatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chorus with ID: {Id}. Exception: {ExceptionType}", id, ex.GetType().Name);
            return false;
        }
    }
} 