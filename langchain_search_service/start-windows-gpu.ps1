# CHAP2 LangChain Service - Windows GPU Setup
# PowerShell script for deploying with NVIDIA GPU support

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CHAP2 LangChain Service - Windows GPU Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Docker Desktop is running
Write-Host "Checking prerequisites..." -ForegroundColor Yellow
try {
    $dockerVersion = docker version 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Docker Desktop is not running or not installed." -ForegroundColor Red
        Write-Host "Please start Docker Desktop and try again." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
    Write-Host "✓ Docker Desktop is running" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Docker Desktop is not available." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Check NVIDIA GPU support
Write-Host ""
Write-Host "Checking NVIDIA GPU support..." -ForegroundColor Yellow
try {
    $gpuTest = docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ NVIDIA GPU support detected" -ForegroundColor Green
        Write-Host "GPU Information:" -ForegroundColor Cyan
        Write-Host $gpuTest -ForegroundColor Gray
    } else {
        Write-Host "⚠ WARNING: NVIDIA GPU support not detected." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To enable GPU acceleration, you need to:" -ForegroundColor Yellow
        Write-Host "1. Install NVIDIA Container Toolkit" -ForegroundColor White
        Write-Host "2. Enable GPU support in Docker Desktop" -ForegroundColor White
        Write-Host ""
        Write-Host "Instructions:" -ForegroundColor Yellow
        Write-Host "- Download NVIDIA Container Toolkit from: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html" -ForegroundColor White
        Write-Host "- Enable 'Use the WSL 2 based engine' in Docker Desktop settings" -ForegroundColor White
        Write-Host "- Enable 'Use GPU acceleration' in Docker Desktop settings" -ForegroundColor White
        Write-Host ""
        Write-Host "Continuing without GPU support..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ WARNING: Could not test GPU support. Continuing without GPU acceleration." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Starting services with GPU support..." -ForegroundColor Yellow
Write-Host ""

# Copy data if it doesn't exist
if (-not (Test-Path "data")) {
    Write-Host "Copying data directory..." -ForegroundColor Yellow
    try {
        Copy-Item -Path "..\..\data" -Destination "data" -Recurse -Force
        Write-Host "✓ Data directory copied successfully" -ForegroundColor Green
    } catch {
        Write-Host "⚠ WARNING: Could not copy data directory. Please ensure data exists." -ForegroundColor Yellow
    }
}

# Start Qdrant and Ollama
Write-Host "Starting Qdrant and Ollama containers..." -ForegroundColor Yellow
docker-compose up -d qdrant ollama

Write-Host ""
Write-Host "Waiting for Ollama to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host ""
Write-Host "Pulling Ollama models (this may take a while on first run)..." -ForegroundColor Yellow
Write-Host ""

# Pull the embedding model
Write-Host "Pulling nomic-embed-text model..." -ForegroundColor Cyan
docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text

# Pull the LLM model
Write-Host "Pulling mistral model..." -ForegroundColor Cyan
docker exec langchain_search_service-ollama-1 ollama pull mistral

Write-Host ""
Write-Host "Starting LangChain service..." -ForegroundColor Yellow
docker-compose up -d langchain-service

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Services are starting up..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Qdrant: http://localhost:6333" -ForegroundColor White
Write-Host "Ollama: http://localhost:11434" -ForegroundColor White
Write-Host "LangChain Service: http://localhost:8000" -ForegroundColor White
Write-Host ""
Write-Host "To view logs: docker-compose logs -f" -ForegroundColor Gray
Write-Host "To stop services: docker-compose down" -ForegroundColor Gray
Write-Host ""

# Test GPU availability
Write-Host "Testing GPU availability in Ollama..." -ForegroundColor Yellow
try {
    $ollamaList = docker exec langchain_search_service-ollama-1 ollama list
    Write-Host $ollamaList -ForegroundColor Gray
} catch {
    Write-Host "⚠ Could not test Ollama models. Services may still be starting." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Setup complete! The services are now running with GPU acceleration." -ForegroundColor Green
Write-Host ""
Write-Host "To test the service, try:" -ForegroundColor Cyan
Write-Host "curl -X POST http://localhost:8000/search_intelligent -H 'Content-Type: application/json' -d '{\"query\": \"praise\", \"k\": 2}'" -ForegroundColor Gray
Write-Host ""
Read-Host "Press Enter to exit" 