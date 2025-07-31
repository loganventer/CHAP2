@echo off
echo ========================================
echo Testing Service Connections
echo ========================================
echo.

echo Testing Web Portal (http://localhost:5000):
curl -s -I http://localhost:5000
if %errorlevel% equ 0 (
    echo ✅ Web Portal is accessible
) else (
    echo ❌ Web Portal is not accessible
)
echo.

echo Testing API (http://localhost:5001):
curl -s -I http://localhost:5001
if %errorlevel% equ 0 (
    echo ✅ API is accessible
) else (
    echo ❌ API is not accessible
)
echo.

echo Testing LangChain Service (http://localhost:8000):
curl -s -I http://localhost:8000
if %errorlevel% equ 0 (
    echo ✅ LangChain Service is accessible
) else (
    echo ❌ LangChain Service is not accessible
)
echo.

echo Testing Qdrant (http://localhost:6333):
curl -s -I http://localhost:6333
if %errorlevel% equ 0 (
    echo ✅ Qdrant is accessible
) else (
    echo ❌ Qdrant is not accessible
)
echo.

echo Testing Ollama (http://localhost:11434):
curl -s -I http://localhost:11434
if %errorlevel% equ 0 (
    echo ✅ Ollama is accessible
) else (
    echo ❌ Ollama is not accessible
)
echo.

echo ========================================
echo Port Status
echo ========================================
netstat -an | findstr ":5000"
netstat -an | findstr ":5001"
netstat -an | findstr ":8000"
netstat -an | findstr ":6333"
netstat -an | findstr ":11434"
echo.

pause 