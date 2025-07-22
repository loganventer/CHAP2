using CHAP2.Console.Vectorize.Configuration;
using CHAP2.Console.Vectorize.DTOs;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace CHAP2.Console.Vectorize.Services;

public class QdrantService : IQdrantService
{
    private readonly QdrantSettings _settings;
    private readonly ILogger<QdrantService> _logger;
    private QdrantClient? _client;

    public QdrantService(QdrantSettings settings, ILogger<QdrantService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Qdrant client connects to gRPC port (6334) by default
            _client = new QdrantClient(_settings.Host, _settings.Port + 1); // Use gRPC port
            _logger.LogInformation("Initialized Qdrant client for collection: {CollectionName}", _settings.CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Qdrant client");
            throw;
        }
    }

    public async Task<bool> CollectionExistsAsync()
    {
        try
        {
            var collections = await _client!.ListCollectionsAsync();
            return collections.Any(c => c == _settings.CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if collection exists. Make sure Qdrant is running with: docker-compose up -d");
            return false;
        }
    }

    public async Task CreateCollectionAsync()
    {
        try
        {
            var vectorParams = new VectorParams
            {
                Size = (ulong)_settings.VectorSize,
                Distance = Distance.Cosine
            };

            await _client!.CreateCollectionAsync(_settings.CollectionName, vectorParams);
            _logger.LogInformation("Created Qdrant collection: {CollectionName}", _settings.CollectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Qdrant collection. Make sure Qdrant is running with: docker-compose up -d");
            throw;
        }
    }

    public async Task UpsertVectorsAsync(List<ChorusDataDto> chorusData, List<List<float>> embeddings)
    {
        try
        {
            var points = new List<PointStruct>();
            
            for (int i = 0; i < chorusData.Count; i++)
            {
                var chorus = chorusData[i];
                var embedding = embeddings[i];
                
                var payload = new Dictionary<string, Value>
                {
                    { "name", chorus.Name },
                    { "chorusText", chorus.ChorusText },
                    { "key", chorus.Key },
                    { "type", chorus.Type },
                    { "timeSignature", chorus.TimeSignature },
                    { "createdAt", chorus.CreatedAt.ToString("O") }
                };

                var point = new PointStruct
                {
                    Id = new PointId { Uuid = chorus.Id },
                    Vectors = new Vectors { Vector = new Vector { Data = { embedding } } },
                    Payload = { payload }
                };
                
                points.Add(point);
            }

            await _client!.UpsertAsync(_settings.CollectionName, points);
            _logger.LogInformation("Upserted {Count} vectors to Qdrant", points.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting vectors to Qdrant");
            throw;
        }
    }
} 