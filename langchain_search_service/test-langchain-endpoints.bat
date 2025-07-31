@echo off
echo ========================================
echo Testing LangChain Service Endpoints
echo ========================================
echo.

echo Testing LangChain health endpoint:
curl -s http://localhost:8000/health
echo.

echo Testing LangChain search endpoint:
curl -s -X POST http://localhost:8000/search -H "Content-Type: application/json" -d "{\"query\":\"test\",\"k\":5}"
echo.

echo Testing LangChain intelligent search endpoint:
curl -s -X POST http://localhost:8000/search_intelligent -H "Content-Type: application/json" -d "{\"query\":\"test\",\"k\":5}"
echo.

echo Testing LangChain streaming endpoint:
curl -s -X POST http://localhost:8000/search_intelligent_stream -H "Content-Type: application/json" -d "{\"query\":\"test\",\"k\":5}" --max-time 5
echo.

echo ========================================
echo Testing Ollama Connection from LangChain
echo ========================================
echo.

echo Testing if LangChain can reach Ollama:
curl -s -X POST http://localhost:8000/test_ollama
echo.

echo ========================================
echo Container Logs (Last 10 lines)
echo ========================================
docker-compose -f docker-compose.host-ollama-network.yml logs langchain-service --tail=10
echo.

pause 