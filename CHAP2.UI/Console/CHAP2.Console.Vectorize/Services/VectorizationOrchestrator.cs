using CHAP2.Console.Vectorize.Configuration;
using CHAP2.Console.Vectorize.DTOs;
using Microsoft.Extensions.Logging;

namespace CHAP2.Console.Vectorize.Services;

public class VectorizationOrchestrator : IVectorizationOrchestrator
{
    private readonly IChorusDataService _chorusDataService;
    private readonly IVectorizationService _vectorizationService;
    private readonly IQdrantService _qdrantService;
    private readonly VectorizationSettings _settings;
    private readonly ILogger<VectorizationOrchestrator> _logger;

    public VectorizationOrchestrator(
        IChorusDataService chorusDataService,
        IVectorizationService vectorizationService,
        IQdrantService qdrantService,
        VectorizationSettings settings,
        ILogger<VectorizationOrchestrator> logger)
    {
        _chorusDataService = chorusDataService;
        _vectorizationService = vectorizationService;
        _qdrantService = qdrantService;
        _settings = settings;
        _logger = logger;
    }

    public async Task VectorizeChorusDataAsync(string dataPath)
    {
        try
        {
            _logger.LogInformation("Starting vectorization process for data path: {DataPath}", dataPath);

            // Initialize Qdrant
            await _qdrantService.InitializeAsync();

            // Check if collection exists, create if not
            if (!await _qdrantService.CollectionExistsAsync())
            {
                _logger.LogInformation("Qdrant collection does not exist, creating...");
                await _qdrantService.CreateCollectionAsync();
            }

            // Load chorus data
            var chorusData = await _chorusDataService.LoadChorusDataAsync(dataPath);
            
            if (chorusData.Count == 0)
            {
                _logger.LogWarning("No chorus data found to vectorize");
                return;
            }

            _logger.LogInformation("Processing {Count} chorus records", chorusData.Count);

            // Prepare texts for vectorization
            var texts = chorusData.Select(c => $"{c.Name}\n{c.ChorusText}").ToList();

            // Generate embeddings in batches
            var allEmbeddings = new List<List<float>>();
            
            for (int i = 0; i < texts.Count; i += _settings.BatchSize)
            {
                var batch = texts.Skip(i).Take(_settings.BatchSize).ToList();
                _logger.LogInformation("Processing batch {BatchNumber} of {TotalBatches}", 
                    (i / _settings.BatchSize) + 1, 
                    (texts.Count + _settings.BatchSize - 1) / _settings.BatchSize);

                var batchEmbeddings = await _vectorizationService.GenerateEmbeddingsAsync(batch);
                allEmbeddings.AddRange(batchEmbeddings);

                // Add delay between batches to avoid rate limits
                if (i + _settings.BatchSize < texts.Count)
                {
                    await Task.Delay(_settings.RetryDelayMs);
                }
            }

            // Store vectors in Qdrant
            await _qdrantService.UpsertVectorsAsync(chorusData, allEmbeddings);

            _logger.LogInformation("Successfully vectorized and stored {Count} chorus records", chorusData.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during vectorization process");
            throw;
        }
    }
} 