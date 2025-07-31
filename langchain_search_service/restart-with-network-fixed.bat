@echo off
echo ========================================
echo Restarting with Network-Fixed Configuration
echo ========================================
echo.

echo Stopping all existing containers...
docker-compose -f docker-compose.hybrid-approach.yml down
docker-compose -f docker-compose.host-ollama.yml down
docker-compose -f docker-compose.host-ollama-network.yml down
docker-compose -f docker-compose.host-ollama-windows.yml down
docker-compose -f docker-compose.host-ollama-custom-network.yml down
docker-compose -f docker-compose.host-ollama-network-fixed.yml down
echo.

echo Starting with network-fixed configuration...
docker-compose -f docker-compose.host-ollama-network-fixed.yml up -d
echo.

echo Waiting 20 seconds for services to start...
timeout /t 20 /nobreak >nul
echo.

echo Checking container status:
docker ps
echo.

echo Testing connections:
echo.

echo Testing Web Portal (should be on port 5000):
curl -s -I http://localhost:5000
echo.

echo Testing API (should be on port 5001):
curl -s -I http://localhost:5001
echo.

echo Testing LangChain Service (should be on port 8000):
curl -s -I http://localhost:8000
echo.

echo.
echo If services are not responding, check logs with:
echo docker-compose -f docker-compose.host-ollama-network-fixed.yml logs -f
echo.
echo To check LangChain service logs specifically:
echo docker-compose -f docker-compose.host-ollama-network-fixed.yml logs -f langchain-service
echo.
pause 