@echo off
echo ========================================
echo Debugging Configuration Issues
echo ========================================
echo.

echo Checking container status:
docker ps
echo.

echo ========================================
echo Testing Service Health Endpoints
echo ========================================
echo.

echo Testing LangChain health endpoint:
curl -v http://localhost:8000/health
echo.

echo Testing API health endpoint:
curl -v http://localhost:5001/health
echo.

echo Testing Qdrant health endpoint:
curl -v http://localhost:6333/collections
echo.

echo ========================================
echo Checking Container Logs
echo ========================================
echo.

echo LangChain Service Logs (last 20 lines):
docker-compose -f docker-compose.host-ollama-windows-fixed.yml logs langchain-service --tail=20
echo.

echo API Service Logs (last 10 lines):
docker-compose -f docker-compose.host-ollama-windows-fixed.yml logs chap2-api --tail=10
echo.

echo Web Portal Logs (last 10 lines):
docker-compose -f docker-compose.host-ollama-windows-fixed.yml logs chap2-webportal --tail=10
echo.

echo ========================================
echo Testing Network Connectivity
echo ========================================
echo.

echo Testing if LangChain can reach Qdrant:
docker exec langchain_search_service-langchain-service-1 curl -s http://qdrant:6333/collections
echo.

echo Testing if LangChain can reach Ollama:
docker exec langchain_search_service-langchain-service-1 curl -s http://172.17.0.1:11434/api/tags
echo.

echo ========================================
echo Checking Environment Variables
echo ========================================
echo.

echo LangChain Service Environment:
docker exec langchain_search_service-langchain-service-1 env | findstr -i "ollama\|qdrant"
echo.

echo API Service Environment:
docker exec langchain_search_service-chap2-api-1 env | findstr -i "aspnetcore"
echo.

echo Web Portal Environment:
docker exec langchain_search_service-chap2-webportal-1 env | findstr -i "ollama\|langchain\|api"
echo.

pause 