@echo off
echo Starting CHAP2 with host Ollama configuration...
echo.
echo This configuration will use your host's Ollama service on port 11434
echo Make sure Ollama is running on your host machine before starting!
echo.

REM Check if Ollama is running on the host
echo Checking if Ollama is available on host...
curl -s http://localhost:11434/api/tags >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Ollama is not running on localhost:11434
    echo Please start Ollama on your host machine first
    echo You can start it with: ollama serve
    pause
    exit /b 1
)

echo Ollama is running on host - proceeding with Docker startup...
echo.

REM Stop any existing containers
echo Stopping existing containers...
docker-compose -f docker-compose.host-ollama.yml down

REM Start the services
echo Starting services with host Ollama configuration...
docker-compose -f docker-compose.host-ollama.yml up -d

echo.
echo Services started successfully!
echo.
echo Available endpoints:
echo - Qdrant: http://localhost:6333
echo - LangChain Service: http://localhost:8000
echo - CHAP2 API: http://localhost:5001
echo - CHAP2 Web Portal: http://localhost:5002
echo - Host Ollama: http://localhost:11434
echo.
echo To view logs: docker-compose -f docker-compose.host-ollama.yml logs -f
echo To stop services: docker-compose -f docker-compose.host-ollama.yml down
pause 