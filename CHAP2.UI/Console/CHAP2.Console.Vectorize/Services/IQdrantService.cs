using CHAP2.Console.Vectorize.DTOs;

namespace CHAP2.Console.Vectorize.Services;

public interface IQdrantService
{
    Task InitializeAsync();
    Task UpsertVectorsAsync(List<ChorusDataDto> chorusData, List<List<float>> embeddings);
    Task<bool> CollectionExistsAsync();
    Task CreateCollectionAsync();
} 