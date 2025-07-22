using CHAP2.WebPortal.Configuration;
using CHAP2.WebPortal.DTOs;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CHAP2.WebPortal.Services;

public class OllamaService : IOllamaService
{
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaService> _logger;
    private readonly HttpClient _httpClient;

    public OllamaService(OllamaSettings settings, ILogger<OllamaService> logger, HttpClient httpClient)
    {
        _settings = settings;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri($"http://{_settings.Host}:{_settings.Port}/");
    }

    public async Task<string> GenerateResponseAsync(string prompt)
    {
        try
        {
            var request = new OllamaRequest
            {
                Model = _settings.Model,
                Prompt = prompt,
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = _settings.Temperature,
                    MaxTokens = _settings.MaxTokens,
                    TopP = _settings.TopP,
                    TopK = _settings.TopK,
                    RepeatPenalty = _settings.RepeatPenalty
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _logger.LogDebug("Sending request to Ollama: {Request}", json);
            var response = await _httpClient.PostAsync("api/generate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Raw Ollama response: {Response}", responseContent);
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                
                _logger.LogDebug("Generated response from Ollama for prompt: {Prompt}", prompt);
                return ollamaResponse?.Response ?? "No response generated";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to generate response from Ollama. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to generate response: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from Ollama");
            throw;
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt)
    {
        var request = new OllamaRequest
        {
            Model = _settings.Model,
            Prompt = prompt,
            Stream = true,
            Options = new OllamaOptions
            {
                Temperature = _settings.Temperature,
                MaxTokens = _settings.MaxTokens,
                TopP = _settings.TopP,
                TopK = _settings.TopK,
                RepeatPenalty = _settings.RepeatPenalty
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _httpClient.PostAsync("api/generate", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to generate streaming response from Ollama. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new Exception($"Failed to generate streaming response: {response.StatusCode} - {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrEmpty(line))
            {
                OllamaResponse? ollamaResponse = null;
                try
                {
                    ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming response line: {Line}", line);
                    continue;
                }
                
                if (ollamaResponse?.Response != null)
                {
                    yield return ollamaResponse.Response;
                }
                
                if (ollamaResponse?.Done == true)
                {
                    break;
                }
            }
        }
    }
} 