# CHAP2 Console Vectorize

This console application vectorizes chorus data from JSON files and stores the embeddings in Qdrant (free local vector database).

## Prerequisites

1. **Docker**: Install Docker Desktop or Docker Engine
2. **.NET 9.0**: Make sure you have .NET 9.0 SDK installed

## Setup

### 1. Start Qdrant with Docker

Start the Qdrant vector database:

```bash
docker-compose up -d
```

This will start Qdrant on `localhost:6333`.

### 2. Configure Qdrant (Optional)

Edit `appsettings.json` if you need to change the default settings:

```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "chorus-vectors",
    "VectorSize": 1536
  }
}
```

### 3. Build the Project

```bash
dotnet build
```

## Usage

### Basic Usage

```bash
dotnet run
```

This will use the default data path: `../../../CHAP2.Chorus.Api/data/chorus`

### Custom Data Path

```bash
dotnet run "/path/to/your/chorus/data"
```

## Features

- **Free Vectorization**: Uses hash-based deterministic embeddings (no API costs)
- **Local Vector Database**: Uses Qdrant (free, open-source) running in Docker
- **Batch Processing**: Processes data in configurable batches
- **Error Handling**: Comprehensive error handling and logging
- **Metadata Storage**: Stores chorus metadata alongside vectors
- **Retry Logic**: Configurable retry settings for API calls

## Configuration

### Qdrant Settings

- `Host`: Qdrant host (default: "localhost")
- `Port`: Qdrant port (default: 6333)
- `CollectionName`: Name of the Qdrant collection (default: "chorus-vectors")
- `VectorSize`: Vector dimension (default: 1536)

### Vectorization Settings

- `Method`: Vectorization method (default: "hash-based")
- `Dimension`: Vector dimension (default: 1536)
- `BatchSize`: Number of records to process in each batch (default: 100)
- `MaxRetries`: Maximum number of retry attempts (default: 3)
- `RetryDelayMs`: Delay between retries in milliseconds (default: 1000)

## Data Format

The application expects JSON files with the following structure:

```json
{
  "id": "unique-id",
  "name": "Chorus Name",
  "chorusText": "Chorus lyrics...",
  "key": 0,
  "type": 0,
  "timeSignature": 0,
  "createdAt": "2025-07-18T14:26:02.378452Z",
  "metadata": {},
  "domainEvents": []
}
```

## Vector Generation

The application combines the chorus name and text for vectorization:

```
"{name}\n{chorusText}"
```

This creates embeddings that can be used for semantic search across both titles and content.

## Output

Vectors are stored in Qdrant with the following metadata:
- `name`: Chorus name
- `chorusText`: Full chorus text
- `key`: Musical key
- `type`: Chorus type
- `timeSignature`: Time signature
- `createdAt`: Creation timestamp

## Docker Commands

```bash
# Start Qdrant
docker-compose up -d

# Stop Qdrant
docker-compose down

# View logs
docker-compose logs -f qdrant

# Access Qdrant UI (optional)
# Open http://localhost:6333/dashboard in your browser
``` 