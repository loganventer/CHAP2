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

echo Choose a configuration option:
echo 1. Standard host.docker.internal (recommended for Windows)
echo 2. Network mode host (alternative approach)
echo 3. Use extra_hosts configuration
echo 4. Custom network with host gateway (recommended if others fail)
echo.
set /p choice="Enter your choice (1-4): "

if "%choice%"=="1" (
    echo Using standard host.docker.internal configuration...
    set compose_file=docker-compose.host-ollama.yml
) else if "%choice%"=="2" (
    echo Using network_mode host configuration...
    set compose_file=docker-compose.host-ollama-network.yml
) else if "%choice%"=="3" (
    echo Using extra_hosts configuration...
    set compose_file=docker-compose.host-ollama-windows.yml
) else if "%choice%"=="4" (
    echo Using custom network with host gateway...
    set compose_file=docker-compose.host-ollama-custom-network.yml
) else (
    echo Invalid choice. Using default configuration...
    set compose_file=docker-compose.host-ollama.yml
)

REM Stop any existing containers
echo Stopping existing containers...
docker-compose -f %compose_file% down

REM Start the services
echo Starting services with host Ollama configuration...
docker-compose -f %compose_file% up -d

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
echo To view logs: docker-compose -f %compose_file% logs -f
echo To stop services: docker-compose -f %compose_file% down
echo.
echo If you're still having connection issues, try:
echo 1. Restart Docker Desktop
echo 2. Check Windows Firewall settings
echo 3. Try the network_mode host option (choice 2)
pause 