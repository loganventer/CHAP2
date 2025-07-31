@echo off
echo ========================================
echo Windows Deployment Server Restart
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
echo.

echo Starting with Windows deployment configuration...
docker-compose -f docker-compose.windows-deployment.yml up -d
echo.

echo Waiting 25 seconds for services to start...
timeout /t 25 /nobreak >nul
echo.

echo Checking container status:
docker ps
echo.

echo Testing local connections:
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
echo Getting server IP address:
ipconfig | findstr "IPv4"
echo.

echo.
echo IMPORTANT: For external access, use the server's IP address:
echo - Web Portal: http://SERVER_IP:5000
echo - API: http://SERVER_IP:5001
echo - LangChain: http://SERVER_IP:8000
echo.
echo If you can't access from another machine:
echo 1. Check Windows Firewall settings
echo 2. Ensure ports 5000, 5001, 8000, 6333 are open
echo 3. Check if server is behind a corporate firewall
echo.
echo To check logs:
echo docker-compose -f docker-compose.windows-deployment.yml logs -f
echo.
pause 