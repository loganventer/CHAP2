using CHAP2.Console.Vectorize.DTOs;

namespace CHAP2.Console.Vectorize.Services;

public interface IVectorizationService
{
    Task<List<float>> GenerateEmbeddingAsync(string text);
    Task<List<List<float>>> GenerateEmbeddingsAsync(List<string> texts);
} 