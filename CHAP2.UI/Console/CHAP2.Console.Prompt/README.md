# CHAP2 Console Prompt

A conversational AI console app that uses Ollama with Llama 3 to answer questions about your vectorized chorus data.

## Features

- **Natural Language Queries**: Ask questions about your chorus collection
- **Vector Search**: Uses semantic search to find relevant choruses
- **LLM Integration**: Ollama with Llama 3 for intelligent responses
- **Interactive Console**: Command-line interface for easy interaction

## Prerequisites

1. **Ollama**: Install from [ollama.ai](https://ollama.ai)
2. **Llama 3 Model**: Pull the model with `ollama pull llama3.2`
3. **Qdrant**: Must be running (from vectorization app)
4. **.NET 9.0**: SDK installed

## Setup

### 1. Install Ollama

```bash
# macOS
curl -fsSL https://ollama.ai/install.sh | sh

# Or download from https://ollama.ai
```

### 2. Pull Llama 3 Model

```bash
ollama pull llama3.2
```

### 3. Start Qdrant (if not running)

```bash
cd ../CHAP2.Console.Vectorize
docker-compose up -d
```

### 4. Build and Run

```bash
dotnet build
dotnet run
```

## Usage

### Interactive Mode

```bash
dotnet run
```

Then you can:

- **Ask questions**: `What choruses mention Jesus?`
- **Search directly**: `search love`
- **Exit**: `exit`

### Examples

```
> What choruses mention Jesus?
> search hallelujah
> Which choruses are about love?
> Find choruses with themes of salvation
> search joy
```

## Configuration

Edit `appsettings.json` to customize:

```json
{
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "chorus-vectors"
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "llama3.2",
    "MaxTokens": 2048,
    "Temperature": 0.7
  },
  "Prompt": {
    "MaxResults": 5,
    "SimilarityThreshold": 0.7
  }
}
```

## How It Works

1. **Question Processing**: Your question is converted to a vector
2. **Vector Search**: Finds similar choruses in Qdrant
3. **Context Building**: Relevant choruses are formatted as context
4. **LLM Generation**: Ollama generates a response using the context
5. **Response**: Returns an intelligent answer based on your data

## Commands

- `search <query>` - Direct vector search without LLM
- `exit` - Quit the application
- Any other text - Ask a question about your choruses

## Troubleshooting

- **Ollama not found**: Make sure Ollama is installed and running
- **Model not found**: Run `ollama pull llama3.2`
- **Qdrant connection error**: Start Qdrant with `docker-compose up -d`
- **No results**: Try rephrasing your question or using different keywords 