using CHAP2.Console.Prompt.Configuration;
using CHAP2.Console.Prompt.DTOs;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CHAP2.Console.Prompt.Services;

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
                    MaxTokens = _settings.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("api/generate", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
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
} 