@echo off
echo ========================================
echo Finding Host IP and Testing Ollama Connection
echo ========================================
echo.

echo Getting Docker host IP...
for /f "tokens=2 delims=:" %%i in ('docker network inspect bridge ^| findstr "Gateway"') do set DOCKER_HOST_IP=%%i
set DOCKER_HOST_IP=%DOCKER_HOST_IP: =%
echo Docker Host IP: %DOCKER_HOST_IP%
echo.

echo Testing Ollama connection from host...
curl -s http://localhost:11434/api/tags
echo.

echo Testing Ollama connection using Docker host IP...
curl -s http://%DOCKER_HOST_IP%:11434/api/tags
echo.

echo ========================================
echo Testing with Different IP Addresses
echo ========================================
echo.

echo Testing with 172.17.0.1 (default Docker bridge):
curl -s http://172.17.0.1:11434/api/tags
echo.

echo Testing with 10.0.2.2 (Docker Desktop default):
curl -s http://10.0.2.2:11434/api/tags
echo.

echo Testing with host.docker.internal:
curl -s http://host.docker.internal:11434/api/tags
echo.

echo ========================================
echo Creating Test Container
echo ========================================
echo.

echo Creating a test container to verify Ollama connectivity...
docker run --rm -it --network chap2-network alpine sh -c "apk add --no-cache curl && curl -s http://172.17.0.1:11434/api/tags"
echo.

echo ========================================
echo Restarting with Correct Host IP
echo ========================================
echo.

echo Stopping current containers...
docker-compose -f docker-compose.host-ollama-custom-network.yml down
docker-compose -f docker-compose.host-ollama-windows-fixed.yml down
echo.

echo Starting with Windows-fixed configuration...
docker-compose -f docker-compose.host-ollama-windows-fixed.yml up -d
echo.

echo Waiting 15 seconds for services to start...
timeout /t 15 /nobreak >nul
echo.

echo Testing connections...
test-connections.bat
echo.

pause 