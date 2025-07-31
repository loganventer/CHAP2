@echo off
echo ========================================
echo Windows Deployment Fixed Restart
echo ========================================
echo.

echo Stopping all existing containers...
docker-compose -f docker-compose.hybrid-approach.yml down
docker-compose -f docker-compose.host-ollama.yml down
docker-compose -f docker-compose.host-ollama-network.yml down
docker-compose -f docker-compose.host-ollama-windows.yml down
docker-compose -f docker-compose.host-ollama-custom-network.yml down
docker-compose -f docker-compose.host-ollama-network-fixed.yml down
docker-compose -f docker-compose.windows-deployment.yml down
docker-compose -f docker-compose.windows-deployment-fixed.yml down
echo.

echo Starting with fixed Windows deployment configuration...
docker-compose -f docker-compose.windows-deployment-fixed.yml up -d
echo.

echo Waiting 30 seconds for services to start...
timeout /t 30 /nobreak >nul
echo.

echo Checking container status:
docker ps
echo.

echo Testing connections on host IP (192.168.0.27):
echo.

echo Testing Web Portal (port 5000):
curl -s -I http://192.168.0.27:5000
echo.

echo Testing API (port 5001):
curl -s -I http://192.168.0.27:5001
echo.

echo Testing LangChain Service (port 8000):
curl -s -I http://192.168.0.27:8000/health
echo.

echo Testing Qdrant (port 6333):
curl -s -I http://192.168.0.27:6333
echo.

echo.
echo Access URLs:
echo - Web Portal: http://192.168.0.27:5000
echo - API: http://192.168.0.27:5001
echo - LangChain: http://192.168.0.27:8000
echo - Qdrant: http://192.168.0.27:6333
echo.

echo If services still fail, check:
echo 1. Windows Firewall allows these ports
echo 2. Docker Desktop is running
echo 3. No other services are using these ports
echo.

echo To check logs:
echo docker-compose -f docker-compose.windows-deployment-fixed.yml logs -f
echo.

pause 