@echo off
echo ========================================
echo CHAP2 LangChain Search Service Deployment
echo ========================================
echo.

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not available
    echo Please ensure PowerShell is installed and accessible
    pause
    exit /b 1
)

REM Check if the PowerShell script exists
if not exist "start-windows-gpu-detection.ps1" (
    echo ERROR: start-windows-gpu-detection.ps1 not found
    echo Please ensure you are running this from the correct directory
    pause
    exit /b 1
)

echo Starting deployment with PowerShell...
echo.

REM Execute the PowerShell script with all arguments
powershell -ExecutionPolicy Bypass -File "start-windows-gpu-detection.ps1" %*

echo.
echo Deployment script completed.
pause

 