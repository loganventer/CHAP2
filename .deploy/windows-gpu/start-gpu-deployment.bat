@echo off
echo ========================================
echo CHAP2 Windows GPU Deployment
echo ========================================
echo.

echo Checking Docker and NVIDIA Container Toolkit...
docker --version
nvidia-smi
echo.

echo Stopping any existing containers...
docker-compose down
echo.

echo Building and starting services with GPU support...
docker-compose up -d --build
echo.

echo Waiting 30 seconds for services to start...
timeout /t 30 /nobreak >nul
echo.

echo Checking container status:
docker ps
echo.

echo Testing GPU access in LangChain container:
docker exec -it $(docker ps -q --filter "name=langchain-service") nvidia-smi
echo.

echo Getting server IP address:
ipconfig | findstr "IPv4"
echo.

echo Testing connections:
echo.

echo Testing Web Portal (port 5000):
curl -s -I http://localhost:5000
echo.

echo Testing API (port 5001):
curl -s -I http://localhost:5001
echo.

echo Testing LangChain Service (port 8000):
curl -s -I http://localhost:8000/health
echo.

echo Testing Qdrant (port 6333):
curl -s -I http://localhost:6333
echo.

echo.
echo Access URLs (replace SERVER_IP with your actual IP):
echo - Web Portal: http://SERVER_IP:5000
echo - API: http://SERVER_IP:5001
echo - LangChain: http://SERVER_IP:8000
echo - Qdrant: http://SERVER_IP:6333
echo.

echo To view logs:
echo docker-compose logs -f
echo.

echo To stop services:
echo docker-compose down
echo.

pause 