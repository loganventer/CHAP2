@echo off
echo ========================================
echo Windows GPU Check for CHAP2
echo ========================================
echo.

echo Checking NVIDIA GPU...
nvidia-smi
if %errorlevel% neq 0 (
    echo ERROR: nvidia-smi not found or GPU not detected
    goto :end
)

echo.
echo Checking Docker GPU runtime...
docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi
if %errorlevel% neq 0 (
    echo ERROR: Docker GPU runtime not working
    goto :end
)

echo.
echo Checking Ollama container GPU access...
docker exec -it langchain_search_service-ollama-1 nvidia-smi 2>nul
if %errorlevel% neq 0 (
    echo ERROR: Ollama container cannot access GPU
    goto :end
)

echo.
echo Checking Ollama models...
docker exec -it langchain_search_service-ollama-1 ollama list

echo.
echo Testing GPU inference...
docker exec -it langchain_search_service-ollama-1 ollama run mistral "Test GPU inference" 2>nul

:end
echo.
echo GPU check complete!
pause 