@echo off
echo ========================================
echo Docker Container Status Check
echo ========================================
echo.

echo Checking running containers:
docker ps
echo.

echo Checking all containers (including stopped):
docker ps -a
echo.

echo ========================================
echo Container Logs
echo ========================================
echo.

echo LangChain Service Logs:
docker-compose -f docker-compose.host-ollama-network.yml logs langchain-service --tail=20
echo.

echo API Service Logs:
docker-compose -f docker-compose.host-ollama-network.yml logs chap2-api --tail=20
echo.

echo Web Portal Logs:
docker-compose -f docker-compose.host-ollama-network.yml logs chap2-webportal --tail=20
echo.

echo ========================================
echo Port Status
echo ========================================
echo.

echo Checking if containers are binding to ports:
netstat -an | findstr ":5000"
netstat -an | findstr ":5001"
netstat -an | findstr ":8000"
echo.

echo ========================================
echo Docker Network Info
echo ========================================
echo.

echo Docker networks:
docker network ls
echo.

echo Inspecting default network:
docker network inspect bridge
echo.

pause 