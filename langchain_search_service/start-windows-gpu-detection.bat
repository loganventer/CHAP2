@echo off
setlocal enabledelayedexpansion

echo ========================================
echo CHAP2 LangChain Service - Windows GPU Detection
echo ========================================

REM Check if running as administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: This script may need administrator privileges for GPU setup
    echo If you encounter permission errors, run as Administrator
    echo.
)

REM Check if Docker Desktop is running
echo Checking Docker Desktop...
docker version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker Desktop is not running or not installed.
    echo Please start Docker Desktop and try again.
    pause
    exit /b 1
)
echo ✓ Docker Desktop is running

REM Check for NVIDIA GPU using nvidia-smi
echo Checking for NVIDIA GPU...
nvidia-smi >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ NVIDIA GPU detected
    set GPU_AVAILABLE=1
    echo.
    echo NVIDIA GPU Information:
    nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits
    echo.
    
    REM Check NVIDIA drivers
    echo Checking NVIDIA drivers...
    nvidia-smi --query-gpu=driver_version --format=csv,noheader >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ NVIDIA drivers are installed
        set DRIVERS_INSTALLED=1
    ) else (
        echo ⚠ NVIDIA drivers not detected
        set DRIVERS_INSTALLED=0
        echo.
        echo NVIDIA drivers are required for GPU acceleration.
        echo Please install NVIDIA drivers from: https://www.nvidia.com/Download/index.aspx
        echo After installing drivers, restart this script.
        echo.
        set /p CONTINUE="Continue without GPU support? (y/n): "
        if /i not "%CONTINUE%"=="y" (
            echo Installation cancelled.
            pause
            exit /b 1
        )
        set GPU_AVAILABLE=0
    )
) else (
    echo ⚠ No NVIDIA GPU detected or nvidia-smi not available
    set GPU_AVAILABLE=0
    set DRIVERS_INSTALLED=0
)

REM Check if NVIDIA Container Toolkit is available
echo Checking NVIDIA Container Toolkit...
docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ NVIDIA Container Toolkit is available
    set CONTAINER_GPU=1
) else (
    echo ⚠ NVIDIA Container Toolkit not available
    set CONTAINER_GPU=0
    
    REM Offer to install Container Toolkit if GPU is available
    if %GPU_AVAILABLE%==1 (
        echo.
        echo NVIDIA Container Toolkit is required for GPU acceleration in Docker.
        echo.
        set /p INSTALL_TOOLKIT="Install NVIDIA Container Toolkit automatically? (y/n): "
        if /i "%INSTALL_TOOLKIT%"=="y" (
            echo Installing NVIDIA Container Toolkit...
            
            REM Check if running as administrator
            net session >nul 2>&1
            if %errorlevel% neq 0 (
                echo ERROR: Administrator privileges required to install NVIDIA Container Toolkit
                echo Please run this script as Administrator
                pause
                exit /b 1
            )
            
            REM Download and install NVIDIA Container Toolkit
            echo Downloading NVIDIA Container Toolkit installer...
            powershell -Command "& {Invoke-WebRequest -Uri 'https://nvidia.github.io/libnvidia-container/windows/nvidia-container-toolkit-windows-latest.exe' -OutFile '%TEMP%\nvidia-container-toolkit-installer.exe'}"
            
            if exist "%TEMP%\nvidia-container-toolkit-installer.exe" (
                echo Installing NVIDIA Container Toolkit...
                "%TEMP%\nvidia-container-toolkit-installer.exe" /S
                
                REM Clean up installer
                del "%TEMP%\nvidia-container-toolkit-installer.exe"
                
                echo ✓ NVIDIA Container Toolkit installed successfully
                echo Please restart Docker Desktop and run this script again.
                pause
                exit /b 0
            ) else (
                echo ERROR: Failed to download NVIDIA Container Toolkit installer
                echo Please install manually from: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
            )
        )
    )
)

REM Configure Docker Desktop for GPU if needed
if %GPU_AVAILABLE%==1 (
    echo Configuring Docker Desktop for GPU support...
    
    REM Try to configure Docker Desktop settings
    powershell -Command "& {
        $settingsPath = '$env:APPDATA\Docker\settings.json'
        if (Test-Path $settingsPath) {
            $settings = Get-Content $settingsPath | ConvertFrom-Json
            $modified = $false
            
            if (-not $settings.gpuAcceleration) {
                $settings.gpuAcceleration = $true
                $modified = $true
                Write-Host '✓ GPU acceleration enabled in Docker Desktop'
            }
            
            if (-not $settings.useWsl2Engine) {
                $settings.useWsl2Engine = $true
                $modified = $true
                Write-Host '✓ WSL 2 engine enabled in Docker Desktop'
            }
            
            if ($modified) {
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
                Write-Host '✓ Docker Desktop configured for GPU support'
            } else {
                Write-Host '✓ Docker Desktop already configured for GPU support'
            }
        } else {
            Write-Host '⚠ Docker Desktop settings file not found'
            Write-Host 'Please manually enable GPU acceleration in Docker Desktop settings'
        }
    }"
)

REM Create appropriate docker-compose override file
if %GPU_AVAILABLE%==1 (
    if %CONTAINER_GPU%==1 (
        echo Creating docker-compose.gpu.yml with GPU support...
        (
            echo version: '3.8'
            echo.
            echo services:
            echo   ollama:
            echo     deploy:
            echo       resources:
            echo         reservations:
            echo           devices:
            echo             - driver: nvidia
            echo               count: all
            echo               capabilities: [gpu]
        ) > docker-compose.gpu.yml
        echo ✓ GPU-enabled configuration created
    ) else (
        echo Creating docker-compose.gpu.yml with GPU support ^(requires NVIDIA Container Toolkit^)...
        (
            echo version: '3.8'
            echo.
            echo services:
            echo   ollama:
            echo     deploy:
            echo       resources:
            echo         reservations:
            echo           devices:
            echo             - driver: nvidia
            echo               count: all
            echo               capabilities: [gpu]
            echo.
            echo # NOTE: This configuration requires NVIDIA Container Toolkit to be installed
            echo # If you see GPU errors, install NVIDIA Container Toolkit from:
            echo # https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
        ) > docker-compose.gpu.yml
        echo ✓ GPU configuration created ^(NVIDIA Container Toolkit required^)
    )
) else (
    echo Creating docker-compose.gpu.yml without GPU support...
    (
        echo version: '3.8'
        echo.
        echo services:
        echo   ollama:
        echo     # No GPU configuration - running on CPU
        echo     # To enable GPU support, install NVIDIA drivers and Container Toolkit
    ) > docker-compose.gpu.yml
    echo ✓ CPU-only configuration created
)

echo.
echo ========================================
echo Starting CHAP2 LangChain Services
echo ========================================

REM Stop any existing containers
echo Stopping existing containers...
docker-compose down >nul 2>&1

REM Start Qdrant and Ollama
echo Starting Qdrant and Ollama containers...
if %GPU_AVAILABLE%==1 (
    echo Using GPU-enabled configuration
    docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d qdrant ollama
) else (
    echo Using CPU-only configuration
    docker-compose up -d qdrant ollama
)

REM Wait for Ollama to be ready
echo Waiting for Ollama to start...
timeout /t 10 /nobreak >nul

REM Pull required models
echo Pulling Ollama models ^(this may take a while on first run^)...
docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
if %errorlevel% neq 0 (
    echo ⚠ Warning: Failed to pull nomic-embed-text model
    echo This may be due to network issues or insufficient disk space
)

docker exec langchain_search_service-ollama-1 ollama pull mistral
if %errorlevel% neq 0 (
    echo ⚠ Warning: Failed to pull mistral model
    echo This may be due to network issues or insufficient disk space
)

REM Start LangChain service
echo Starting LangChain service...
if %GPU_AVAILABLE%==1 (
    docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d langchain-service
) else (
    docker-compose up -d langchain-service
)

REM Wait for services to be ready
echo Waiting for services to be ready...
timeout /t 15 /nobreak >nul

REM Check service status
echo.
echo ========================================
echo Service Status
echo ========================================

echo Checking Qdrant...
curl -s http://localhost:6333/collections >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Qdrant is running on http://localhost:6333
) else (
    echo ✗ Qdrant is not responding
)

echo Checking Ollama...
curl -s http://localhost:11434/api/tags >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Ollama is running on http://localhost:11434
) else (
    echo ✗ Ollama is not responding
)

echo Checking LangChain service...
curl -s http://localhost:8000/health >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ LangChain service is running on http://localhost:8000
) else (
    echo ✗ LangChain service is not responding
)

echo.
echo ========================================
echo Deployment Summary
echo ========================================

if %GPU_AVAILABLE%==1 (
    if %CONTAINER_GPU%==1 (
        echo ✓ GPU-accelerated deployment successful
        echo   - NVIDIA GPU detected and enabled
        echo   - NVIDIA Container Toolkit available
        echo   - Ollama running with GPU acceleration
    ) else (
        echo ⚠ GPU detected but Container Toolkit not available
        echo   - NVIDIA GPU detected
        echo   - Install NVIDIA Container Toolkit for GPU acceleration
        echo   - Currently running on CPU
    )
) else (
    echo ℹ CPU-only deployment successful
    echo   - No NVIDIA GPU detected
    echo   - Ollama running on CPU
    echo   - Performance may be slower than GPU version
)

echo.
echo Services are ready:
echo - Qdrant Vector Database: http://localhost:6333
echo - Ollama LLM Service: http://localhost:11434
echo - LangChain Search Service: http://localhost:8000
echo.
echo To start the web portal, run:
echo dotnet run --project CHAP2.UI/CHAP2.WebPortal/CHAP2.Web.csproj --urls "http://localhost:5000"
echo.
echo Press any key to exit...
pause >nul 