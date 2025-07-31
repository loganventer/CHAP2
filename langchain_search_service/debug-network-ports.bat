@echo off
echo ========================================
echo Docker Network and Port Diagnostics
echo ========================================
echo.

echo Checking if ports are in use...
echo.

echo Port 5000 (Web Portal):
netstat -an | findstr :5000
echo.

echo Port 5001 (API):
netstat -an | findstr :5001
echo.

echo Port 8000 (LangChain Service):
netstat -an | findstr :8000
echo.

echo Port 6333 (Qdrant):
netstat -an | findstr :6333
echo.

echo Port 11434 (Ollama):
netstat -an | findstr :11434
echo.

echo ========================================
echo Docker Container Status
echo ========================================
docker ps -a
echo.

echo ========================================
echo Testing Ollama Connection
echo ========================================
curl -s http://localhost:11434/api/tags
echo.

echo ========================================
echo Testing Qdrant Connection
echo ========================================
curl -s http://localhost:6333/collections
echo.

echo ========================================
echo Testing Web Portal Connection
echo ========================================
curl -s -I http://localhost:5000
echo.

echo ========================================
echo Testing API Connection
echo ========================================
curl -s -I http://localhost:5001
echo.

echo ========================================
echo Testing LangChain Service Connection
echo ========================================
curl -s -I http://localhost:8000
echo.

pause 