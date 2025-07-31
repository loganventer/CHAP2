@echo off
echo ========================================
echo Testing LangChain Service Only
echo ========================================
echo.

echo Testing if Web Portal can reach LangChain service:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://host.docker.internal:8000/health
echo.

echo Testing if Web Portal can reach API service:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://chap2-api:5001/health
echo.

echo Testing if Web Portal can reach Qdrant:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://qdrant:6333/collections
echo.

echo ========================================
echo Testing from Windows Host
echo ========================================
echo.

echo Testing LangChain from Windows:
curl -s http://localhost:8000/health
echo.

echo Testing API from Windows:
curl -s -I http://localhost:5001/health
echo.

echo Testing Qdrant from Windows:
curl -s http://localhost:6333/collections
echo.

echo ========================================
echo Testing Web Portal Direct
echo ========================================
echo.

echo Testing Web Portal intelligent search endpoint:
curl -s -X POST http://localhost:5000/Home/IntelligentSearchStream -H "Content-Type: application/json" -d "{\"query\":\"test\",\"maxResults\":5}" --max-time 10
echo.

echo ========================================
echo Container Logs
echo ========================================
echo.

echo Web Portal logs (last 10 lines):
docker-compose -f docker-compose.hybrid-approach.yml logs chap2-webportal --tail=10
echo.

echo LangChain logs (last 5 lines):
docker-compose -f docker-compose.hybrid-approach.yml logs langchain-service --tail=5
echo.

pause 