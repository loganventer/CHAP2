# Quick Start Guide

## 1. Start Qdrant (Free Vector Database)

```bash
# Start Qdrant in Docker
docker-compose up -d

# Verify it's running
docker ps
```

## 2. Run the Vectorization App

```bash
# Build and run
dotnet run

# Or use launch profile with custom data path
dotnet run --launch-profile "CHAP2.Console.Vectorize (Custom Data Path)"

# Or specify a custom data path manually
dotnet run "/path/to/your/chorus/data"
```

## 3. What Happens

1. **Loads Data**: Reads all JSON files from the chorus data directory
2. **Generates Vectors**: Creates 1536-dimensional embeddings using hash-based method (free!)
3. **Stores in Qdrant**: Saves vectors with metadata in the local Qdrant database
4. **Logs Progress**: Shows detailed progress and any errors

## 4. Verify Results

```bash
# Check Qdrant is running
curl http://localhost:6333/collections

# View Qdrant UI (optional)
open http://localhost:6333/dashboard
```

## 5. Stop Qdrant

```bash
docker-compose down
```

## Configuration

Edit `appsettings.json` if needed:

```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "chorus-vectors",
    "VectorSize": 1536
  },
  "Vectorization": {
    "Method": "hash-based",
    "Dimension": 1536,
    "BatchSize": 100,
    "MaxRetries": 3,
    "RetryDelayMs": 1000
  }
}
```

## Features

- ✅ **100% Free**: No API costs, runs locally
- ✅ **Fast**: Hash-based deterministic embeddings
- ✅ **Scalable**: Batch processing for large datasets
- ✅ **Reliable**: Error handling and retry logic
- ✅ **Searchable**: Vectors stored in Qdrant for similarity search

## Troubleshooting

- **Qdrant not starting**: Check Docker is running
- **Connection errors**: Verify Qdrant is on port 6333
- **No data found**: Check the data path contains JSON files 