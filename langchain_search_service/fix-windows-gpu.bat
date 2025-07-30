@echo off
echo ========================================
echo Fix Windows GPU for CHAP2
echo ========================================
echo.

echo Step 1: Stopping containers...
docker-compose -f docker-compose.gpu.yml down

echo.
echo Step 2: Removing existing Ollama models...
docker volume rm langchain_search_service_ollama_models 2>nul
if %errorlevel% neq 0 (
    echo No existing models to remove
)

echo.
echo Step 3: Starting containers with GPU...
docker-compose -f docker-compose.gpu.yml up -d

echo.
echo Step 4: Waiting for services to be ready...
timeout /t 30 /nobreak >nul

echo.
echo Step 5: Pulling models with GPU support...
docker exec -it langchain_search_service-ollama-1 ollama pull mistral
docker exec -it langchain_search_service-ollama-1 ollama pull nomic-embed-text

echo.
echo Step 6: Verifying GPU usage...
docker exec -it langchain_search_service-ollama-1 nvidia-smi 2>nul
if %errorlevel% neq 0 (
    echo WARNING: GPU not detected in container
)

echo.
echo Step 7: Testing GPU inference...
docker exec -it langchain_search_service-ollama-1 ollama run mistral "Test GPU inference" 2>nul

echo.
echo ========================================
echo GPU fix complete!
echo ========================================
echo.
echo Service URLs:
echo - Web Portal: http://localhost:5000
echo - API: http://localhost:5001
echo - LangChain: http://localhost:8000
echo - Qdrant: http://localhost:6333
echo - Ollama: http://localhost:11434
echo.
echo To monitor GPU usage:
echo docker exec -it langchain_search_service-ollama-1 nvidia-smi
echo.
pause 