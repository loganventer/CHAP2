@echo off
echo ========================================
echo Debugging Web Portal Issues
echo ========================================
echo.

echo Starting hybrid approach...
docker-compose -f docker-compose.hybrid-approach.yml up -d
echo.

echo Waiting 20 seconds for services to start...
timeout /t 20 /nobreak >nul
echo.

echo ========================================
echo Testing Web Portal Container
echo ========================================
echo.

echo Testing if Web Portal can reach host.docker.internal:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://host.docker.internal:11434/api/tags
echo.

echo Testing if Web Portal can reach LangChain service:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://langchain-service:8000/health
echo.

echo Testing if Web Portal can reach API service:
docker exec langchain_search_service-chap2-webportal-1 curl -s http://chap2-api:5001/health
echo.

echo ========================================
echo Testing from Windows Host
echo ========================================
echo.

echo Testing Web Portal from Windows:
curl -s -I http://localhost:5000
echo.

echo Testing LangChain from Windows:
curl -s http://localhost:8000/health
echo.

echo Testing API from Windows:
curl -s -I http://localhost:5001/health
echo.

echo ========================================
echo Container Logs
echo ========================================
echo.

echo Web Portal logs (last 20 lines):
docker-compose -f docker-compose.hybrid-approach.yml logs chap2-webportal --tail=20
echo.

echo LangChain logs (last 10 lines):
docker-compose -f docker-compose.hybrid-approach.yml logs langchain-service --tail=10
echo.

echo ========================================
echo Container Status
echo ========================================
docker ps
echo.

pause 