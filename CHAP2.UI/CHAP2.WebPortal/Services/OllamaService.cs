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

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
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
            
            // Use a timeout to prevent hanging requests
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            
            var response = await _httpClient.PostAsync("api/generate", content, combinedCts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(combinedCts.Token);
                _logger.LogDebug("Raw Ollama response: {Response}", responseContent);
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseContent);
                
                _logger.LogDebug("Generated response from Ollama for prompt: {Prompt}", prompt);
                return ollamaResponse?.Response ?? "No response generated";
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(combinedCts.Token);
                _logger.LogError("Failed to generate response from Ollama. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to generate response: {response.StatusCode} - {errorContent}");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ollama request was cancelled by user for prompt: {Prompt}", prompt);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ollama request timed out for prompt: {Prompt}", prompt);
            throw new TimeoutException("Ollama request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from Ollama");
            throw;
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
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
        
        var response = await _httpClient.PostAsync("api/generate", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to generate streaming response from Ollama. Status: {StatusCode}, Error: {Error}", 
                response.StatusCode, errorContent);
            throw new Exception($"Failed to generate streaming response: {response.StatusCode} - {errorContent}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
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