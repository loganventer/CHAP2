@echo off
echo ========================================
echo Trying Different Approaches for Host Ollama Connection
echo ========================================
echo.

echo Approach 1: LangChain with host network mode
echo This puts only the LangChain service on host network
echo.
docker-compose -f docker-compose.windows-network.yml down
docker-compose -f docker-compose.windows-network.yml up -d
echo.

echo Waiting 15 seconds for services to start...
timeout /t 15 /nobreak >nul
echo.

echo Testing LangChain health:
curl -s http://localhost:8000/health
echo.

echo Testing Ollama connection from LangChain container:
docker exec langchain_search_service-langchain-service-1 curl -s http://localhost:11434/api/tags
echo.

echo Testing intelligent search:
curl -s -X POST http://localhost:8000/search_intelligent -H "Content-Type: application/json" -d "{\"query\":\"test\",\"k\":5}" --max-time 10
echo.

echo.
echo If Approach 1 doesn't work, trying Approach 2...
echo.

echo Approach 2: Port forwarding approach
echo.
docker-compose -f docker-compose.windows-network.yml down
docker-compose -f docker-compose.port-forward.yml up -d
echo.

echo Waiting 15 seconds for services to start...
timeout /t 15 /nobreak >nul
echo.

echo Testing LangChain health:
curl -s http://localhost:8000/health
echo.

echo Testing intelligent search:
curl -s -X POST http://localhost:8000/search_intelligent -H "Content-Type: application/json" -d "{\"query\":\"test\",\"k\":5}" --max-time 10
echo.

echo.
echo ========================================
echo Final Test
echo ========================================
echo.

echo Testing all services:
test-connections.bat
echo.

echo If both approaches fail, we may need to:
echo 1. Check Windows Firewall settings
echo 2. Restart Docker Desktop
echo 3. Use a different Docker networking approach
echo.

pause 