# CHAP2 LangChain Search Service

A Python-based search service using LangChain with Ollama and Qdrant for intelligent chorus search with RAG (Retrieval Augmented Generation), caching, and streaming support.

## Features

- **RAG (Retrieval Augmented Generation)**: Combines vector search with LLM analysis
- **GPU Acceleration**: Supports NVIDIA GPUs for faster inference
- **Memory Caching**: In-memory cache for improved performance
- **Streaming Support**: Real-time streaming responses
- **System Prompts**: Structured prompts for consistent LLM output
- **Chaining**: Complex search workflows with LangChain chains
- **Hybrid Search**: Combines dense and sparse vector search

## Prerequisites

### For CPU-only deployment:
- Docker Desktop
- Docker Compose

### For GPU-accelerated deployment (Windows):
- Docker Desktop with WSL 2 backend
- NVIDIA GPU with CUDA support
- NVIDIA Container Toolkit
- NVIDIA GPU drivers

## Quick Start

### CPU Deployment (All Platforms)

```bash
# Clone and navigate to the service directory
cd langchain_search_service

# Start services
./start.sh  # Linux/macOS
# OR
start-windows-gpu.bat  # Windows (CPU mode)
# OR
powershell -ExecutionPolicy Bypass -File start-windows-gpu.ps1  # Windows PowerShell
```

### GPU Deployment (Windows with NVIDIA)

1. **Install NVIDIA Container Toolkit**:
   - Download from: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
   - Follow the Windows installation instructions

2. **Configure Docker Desktop**:
   - Enable "Use the WSL 2 based engine" in Docker Desktop settings
   - Enable "Use GPU acceleration" in Docker Desktop settings
   - Restart Docker Desktop

3. **Start with GPU support**:
   ```cmd
   # Command Prompt
   start-windows-gpu.bat
   
   # OR PowerShell
   powershell -ExecutionPolicy Bypass -File start-windows-gpu.ps1
   ```

## Service Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Web Portal    │    │  LangChain      │    │     Ollama      │
│   (.NET)        │◄──►│   Service       │◄──►│   (LLM +        │
│                 │    │   (Python)      │    │   Embeddings)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                              │
                              ▼
                       ┌─────────────────┐
                       │     Qdrant      │
                       │  (Vector Store) │
                       └─────────────────┘
```

## API Endpoints

### Basic Search
```bash
POST /search
{
  "query": "praise",
  "k": 5
}
```

### Intelligent Search (RAG)
```bash
POST /search_intelligent
{
  "query": "praise",
  "k": 5,
  "include_analysis": true
}
```

### Streaming Intelligent Search
```bash
POST /search_intelligent_stream
{
  "query": "praise",
  "k": 5
}
```

### Add Documents
```bash
POST /add_documents
[
  {
    "id": "chorus-1",
    "text": "Praise Him, praise Him...",
    "name": "Praise Him",
    "key": 0,
    "type": 0
  }
]
```

## Windows GPU Deployment

### Prerequisites

1. **Docker Desktop**: Install and enable Docker Desktop for Windows
2. **NVIDIA GPU**: NVIDIA GPU with CUDA support (optional)
3. **NVIDIA Drivers**: Latest NVIDIA drivers installed (if GPU available)
4. **NVIDIA Container Toolkit**: Install NVIDIA Container Toolkit for Docker (if GPU available)

### Installation Steps

1. **Install NVIDIA Container Toolkit** (if you have an NVIDIA GPU):
   ```bash
   # Download and install from:
   # https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
   ```

2. **Enable GPU Support in Docker Desktop** (if you have an NVIDIA GPU):
   - Open Docker Desktop Settings
   - Go to "Resources" → "WSL Integration" or "Advanced"
   - Enable "Use the WSL 2 based engine"
   - Enable "Use GPU acceleration" if available

### Deployment Scripts

#### Automatic GPU Detection & Installation (Recommended)

**Batch Script**:
```cmd
# Automatically detects GPU and installs requirements
start-windows-gpu-detection.bat
```

**PowerShell Script** (More robust):
```powershell
# Automatically detects GPU and installs requirements
.\start-windows-gpu-detection.ps1

# Force CPU-only mode (ignores GPU detection)
.\start-windows-gpu-detection.ps1 -ForceCPU

# Auto-install all requirements without prompts
.\start-windows-gpu-detection.ps1 -AutoInstall

# Skip all prompts (use with -AutoInstall)
.\start-windows-gpu-detection.ps1 -AutoInstall -SkipPrompts

# Verbose mode for debugging
.\start-windows-gpu-detection.ps1 -Verbose
```

#### Manual GPU Deployment (Legacy)

**Batch Script**:
```cmd
# Manual GPU deployment (requires NVIDIA Container Toolkit)
start-windows-gpu.bat
```

**PowerShell Script**:
```powershell
# Manual GPU deployment (requires NVIDIA Container Toolkit)
.\start-windows-gpu.ps1
```

### GPU Detection & Installation Features

The automatic detection scripts will:

1. **Check Docker Desktop**: Verify Docker is running
2. **Detect NVIDIA GPU**: Use `nvidia-smi` to check for GPU
3. **Check NVIDIA Drivers**: Verify drivers are installed
4. **Install NVIDIA Container Toolkit**: Auto-download and install if missing
5. **Configure Docker Desktop**: Enable GPU acceleration and WSL 2
6. **Create Configuration**: Generate appropriate `docker-compose.gpu.yml`
7. **Deploy Services**: Start with optimal configuration
8. **Verify Status**: Check all services are running

### Automatic Installation Features

- **NVIDIA Drivers**: Detects missing drivers and provides installation links
- **NVIDIA Container Toolkit**: Auto-downloads and installs the latest version
- **Docker Desktop Configuration**: Automatically enables GPU acceleration and WSL 2
- **Administrator Privileges**: Handles permission requirements for installations
- **User Prompts**: Interactive prompts for installation decisions (can be skipped with `-AutoInstall`)

### Deployment Modes

- **GPU-Accelerated**: Full GPU support with NVIDIA Container Toolkit
- **GPU-Detected**: GPU available but Container Toolkit missing (runs on CPU)
- **CPU-Only**: No GPU detected or forced CPU mode
- **Forced CPU**: Manual override to ignore GPU detection

## Configuration

### Environment Variables

- `OLLAMA_URL`: Ollama service URL (default: `http://localhost:11434`)
- `QDRANT_URL`: Qdrant service URL (default: `http://localhost:6333`)

### GPU Configuration

The Ollama container can be configured to use all available NVIDIA GPUs:

```yaml
deploy:
  resources:
    reservations:
      devices:
        - driver: nvidia
          count: all
          capabilities: [gpu]
```

## Performance Benefits

### GPU Acceleration
- **Embeddings**: 5-10x faster with GPU
- **LLM Inference**: 3-5x faster with GPU
- **Batch Processing**: Significant speedup for multiple queries

### Caching
- **Memory Cache**: Instant responses for repeated queries
- **Vector Cache**: Cached similarity search results
- **LLM Cache**: Cached AI analysis responses

## Troubleshooting

### GPU Issues
1. **Check NVIDIA drivers**: `nvidia-smi`
2. **Verify Container Toolkit**: `docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi`
3. **Docker Desktop settings**: Enable WSL 2 and GPU acceleration

### Service Issues
1. **Check logs**: `docker-compose logs -f`
2. **Restart services**: `docker-compose restart`
3. **Rebuild**: `docker-compose build --no-cache`

### Model Issues
1. **Pull models manually**:
   ```bash
   docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
   docker exec langchain_search_service-ollama-1 ollama pull mistral
   ```

## Development

### Local Development
```bash
# Install dependencies
pip install -r requirements.txt

# Run locally
python main.py
```

### Testing
```bash
# Test the service
curl -X POST http://localhost:8000/search_intelligent \
  -H "Content-Type: application/json" \
  -d '{"query": "praise", "k": 2}'
```

## Monitoring

### Health Check
```bash
curl http://localhost:8000/health
```

### GPU Usage
```bash
# Check GPU usage in Ollama container
docker exec langchain_search_service-ollama-1 nvidia-smi
```

### Service Logs
```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f langchain-service
docker-compose logs -f ollama
``` 