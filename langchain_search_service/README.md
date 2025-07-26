# CHAP2 LangChain Search Service

A comprehensive search service for CHAP2 using LangChain, Qdrant vector database, and Ollama for local LLM capabilities.

## Features

- **Vector Search**: Using Qdrant for similarity search
- **Local LLM**: Ollama with Mistral model for AI-powered search
- **RAG (Retrieval Augmented Generation)**: Combines vector search with LLM analysis
- **Memory Cache**: In-memory caching for faster responses
- **Chaining**: LangChain chains for complex search flows
- **System Prompts**: Structured prompts for consistent LLM output
- **Containerized**: Full Docker deployment with GPU support

## Quick Start

### Windows Deployment

1. **Prerequisites**:
   - Docker Desktop installed and running
   - PowerShell 5.1 or later
   - NVIDIA GPU (optional, for GPU acceleration)

2. **Deploy**:
   ```cmd
   start-windows-gpu-detection.bat
   ```

   Or with PowerShell directly:
   ```powershell
   .\start-windows-gpu-detection.ps1
   ```

3. **Access Services**:
   - Web Portal: http://localhost:5000
   - CHAP2 API: http://localhost:5001/api
   - LangChain Service: http://localhost:8000
   - Qdrant: http://localhost:6333
   - Ollama: http://localhost:11434

### Linux/macOS Deployment

1. **Prerequisites**:
   - Docker and Docker Compose installed
   - Make scripts executable: `chmod +x *.sh`

2. **Deploy**:
   ```bash
   ./start.sh
   ```

## Architecture

### Services

- **Qdrant**: Vector database for similarity search
- **Ollama**: Local LLM server with Mistral model
- **LangChain Service**: Python FastAPI service for intelligent search
- **CHAP2 API**: .NET API for chorus data management
- **CHAP2 Web Portal**: .NET web application for user interface

### Data Flow

1. User submits search query via Web Portal
2. Web Portal calls LangChain Service
3. LangChain Service queries Qdrant vector store
4. LangChain Service uses Ollama LLM for analysis
5. Results returned to Web Portal for display

## Configuration

### API Routes

All CHAP2 API endpoints are prefixed with `/api`:
- `/api/health/ping` - Health check
- `/api/choruses` - Get all choruses
- `/api/choruses/search` - Search choruses
- `/api/choruses/{id}` - Get specific chorus

### Environment Variables

- `OLLAMA_URL`: Ollama service URL (default: http://localhost:11434)
- `QDRANT_URL`: Qdrant service URL (default: http://localhost:6333)
- `ASPNETCORE_ENVIRONMENT`: .NET environment (Development/Production)

## GPU Support

### NVIDIA GPU

The deployment script automatically detects NVIDIA GPUs and configures GPU support:

1. **Detection**: Uses `nvidia-smi` to detect GPU
2. **Container Toolkit**: Tests NVIDIA Container Toolkit
3. **GPU Deployment**: Uses GPU-enabled Docker Compose files

### CPU Fallback

If no GPU is detected, the system runs in CPU mode with full functionality.

## Troubleshooting

### Common Issues

1. **Services not accessible**:
   ```powershell
   docker-compose restart
   ```

2. **Search not working**:
   ```powershell
   docker-compose logs -f
   ```

3. **GPU not detected**:
   - Ensure NVIDIA drivers are installed
   - Install NVIDIA Container Toolkit
   - Restart Docker Desktop

4. **Port conflicts**:
   - Check if ports 5000, 5001, 8000, 6333, 11434 are in use
   - Stop conflicting services

### Logs

View service logs:
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f langchain-service
docker-compose logs -f chap2-api
docker-compose logs -f chap2-webportal
```

### Manual Commands

```bash
# Stop all services
docker-compose down

# Rebuild containers
docker-compose build --no-cache

# Start services
docker-compose up -d

# Check container status
docker ps

# Test API endpoints
curl http://localhost:5001/api/health/ping
curl http://localhost:8000/health
```

## Development

### Adding New Features

1. **LangChain Service**: Modify `main.py` for search logic
2. **CHAP2 API**: Add controllers in `CHAP2.Chorus.Api/Controllers/`
3. **Web Portal**: Add views in `CHAP2.UI/CHAP2.WebPortal/Views/`

### Testing

```bash
# Test API endpoints
curl http://localhost:5001/api/choruses

# Test search
curl -X POST http://localhost:8000/search_intelligent \
  -H "Content-Type: application/json" \
  -d '{"query": "test search"}'
```

## File Structure

```
langchain_search_service/
├── main.py                          # LangChain FastAPI service
├── migrate_data.py                  # Data migration script
├── requirements.txt                 # Python dependencies
├── Dockerfile                      # LangChain service container
├── docker-compose.yml              # Main deployment
├── docker-compose.gpu.yml          # GPU deployment
├── docker-compose.gpu-direct.yml   # Direct GPU deployment
├── start-windows-gpu-detection.ps1 # Windows deployment script
├── start-windows-gpu-detection.bat # Windows batch wrapper
├── start.sh                        # Linux/macOS deployment
├── fix-api-routes.ps1              # API route fixes
└── README.md                       # This file
```

## License

This project is part of the CHAP2 system for chorus management and search. 