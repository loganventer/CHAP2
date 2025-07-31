@echo off
echo ========================================
echo Configuring Ollama to Bind to All Interfaces
echo ========================================
echo.

echo Current Ollama status:
ollama list
echo.

echo Stopping Ollama...
ollama stop
echo.

echo Starting Ollama with OLLAMA_HOST=0.0.0.0...
set OLLAMA_HOST=0.0.0.0
ollama serve
echo.

echo Ollama should now be accessible on all interfaces.
echo Test with:
echo curl http://localhost:11434/api/tags
echo curl http://127.0.0.1:11434/api/tags
echo curl http://192.168.0.27:11434/api/tags
echo.

echo If you want to make this permanent, you can:
echo 1. Set environment variable: setx OLLAMA_HOST 0.0.0.0
echo 2. Or create a Windows service with the environment variable
echo.

pause 