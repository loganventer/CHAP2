# Quick Start Guide

## Prerequisites

1. **Install Ollama**: Download from [ollama.ai](https://ollama.ai)
2. **Pull Llama 3**: `ollama pull llama3.2`
3. **Start Qdrant**: Make sure Qdrant is running from the vectorization app

## Setup

### 1. Install Ollama
```bash
# macOS
curl -fsSL https://ollama.ai/install.sh | sh
```

### 2. Pull the Model
```bash
ollama pull llama3.2
```

### 3. Start Qdrant (if not running)
```bash
cd ../CHAP2.Console.Vectorize
docker-compose up -d
```

### 4. Run the Prompt App
```bash
dotnet run
```

## Usage

Once running, you can:

- **Ask questions**: `What choruses mention Jesus?`
- **Search directly**: `search love`
- **Exit**: `exit`

## Example Queries

```
> What choruses mention Jesus?
> search hallelujah
> Which choruses are about love?
> Find choruses with themes of salvation
> search joy
> What choruses talk about heaven?
```

## Troubleshooting

- **"Ollama not found"**: Install Ollama from ollama.ai
- **"Model not found"**: Run `ollama pull llama3.2`
- **"Qdrant connection error"**: Start Qdrant with `docker-compose up -d`
- **No results**: Try rephrasing your question

## Features

✅ **Natural Language Queries** - Ask questions in plain English  
✅ **Vector Search** - Semantic search through your chorus data  
✅ **LLM Integration** - Intelligent responses using Llama 3  
✅ **Interactive Console** - Easy command-line interface  
✅ **Free & Local** - No API costs, runs entirely locally 