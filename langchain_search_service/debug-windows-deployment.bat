@echo off
echo ========================================
echo Windows Deployment Server Diagnostics
echo ========================================
echo.

echo Current Docker containers:
docker ps
echo.

echo Checking if ports are accessible locally:
echo.

echo Testing port 5000 (Web Portal):
netstat -an | findstr :5000
echo.

echo Testing port 5001 (API):
netstat -an | findstr :5001
echo.

echo Testing port 8000 (LangChain):
netstat -an | findstr :8000
echo.

echo Testing port 6333 (Qdrant):
netstat -an | findstr :6333
echo.

echo.
echo Checking container logs:
echo.

echo LangChain Service logs:
docker-compose -f docker-compose.host-ollama-network-fixed.yml logs --tail=20 langchain-service
echo.

echo Web Portal logs:
docker-compose -f docker-compose.host-ollama-network-fixed.yml logs --tail=20 chap2-webportal
echo.

echo API logs:
docker-compose -f docker-compose.host-ollama-network-fixed.yml logs --tail=20 chap2-api
echo.

echo.
echo Network connectivity tests:
echo.

echo Testing localhost connections:
curl -s -I http://localhost:5000 || echo "Web Portal not accessible on localhost:5000"
curl -s -I http://localhost:5001 || echo "API not accessible on localhost:5001"
curl -s -I http://localhost:8000 || echo "LangChain not accessible on localhost:8000"
echo.

echo.
echo To access from another machine, you may need to:
echo 1. Configure Windows Firewall to allow these ports
echo 2. Use the server's IP address instead of localhost
echo 3. Check if the server is behind a firewall/proxy
echo.
echo Common solutions:
echo - Web Portal: http://SERVER_IP:5000
echo - API: http://SERVER_IP:5001
echo - LangChain: http://SERVER_IP:8000
echo.
pause 