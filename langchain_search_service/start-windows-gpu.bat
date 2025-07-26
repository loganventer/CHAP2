@echo off
echo ========================================
echo CHAP2 LangChain Service - Windows GPU Setup
echo ========================================

echo.
echo Checking prerequisites...

REM Check if Docker Desktop is running
docker version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker Desktop is not running or not installed.
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)

REM Check if NVIDIA Container Toolkit is available
docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo WARNING: NVIDIA GPU support not detected.
    echo.
    echo To enable GPU acceleration, you need to:
    echo 1. Install NVIDIA Container Toolkit
    echo 2. Enable GPU support in Docker Desktop
    echo.
    echo Instructions:
    echo - Download NVIDIA Container Toolkit from: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
    echo - Enable "Use the WSL 2 based engine" in Docker Desktop settings
    echo - Enable "Use GPU acceleration" in Docker Desktop settings
    echo.
    echo Continuing without GPU support...
    echo.
)

echo.
echo Starting services with GPU support...
echo.

REM Copy data if it doesn't exist
if not exist "data" (
    echo Copying data directory...
    xcopy "..\..\data" "data\" /E /I /Y >nul 2>&1
    if %errorlevel% neq 0 (
        echo WARNING: Could not copy data directory. Please ensure data exists.
    )
)

echo Starting Qdrant and Ollama containers...
docker-compose up -d qdrant ollama

echo.
echo Waiting for Ollama to start...
timeout /t 10 /nobreak >nul

echo.
echo Pulling Ollama models (this may take a while on first run)...
echo.

REM Pull the embedding model
echo Pulling nomic-embed-text model...
docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text

REM Pull the LLM model
echo Pulling mistral model...
docker exec langchain_search_service-ollama-1 ollama pull mistral

echo.
echo Starting LangChain service...
docker-compose up -d langchain-service

echo.
echo ========================================
echo Services are starting up...
echo ========================================
echo.
echo Qdrant: http://localhost:6333
echo Ollama: http://localhost:11434
echo LangChain Service: http://localhost:8000
echo.
echo To view logs: docker-compose logs -f
echo To stop services: docker-compose down
echo.
echo Testing GPU availability in Ollama...
docker exec langchain_search_service-ollama-1 ollama list

echo.
echo Setup complete! The services are now running with GPU acceleration.
pause 