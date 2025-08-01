@echo off
echo ========================================
echo CHAP2 Force GPU Deployment
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
if not exist "start-force-gpu.ps1" (
    echo ERROR: start-force-gpu.ps1 not found
    echo Please ensure you are running this from the correct directory
    pause
    exit /b 1
)

echo Starting FORCE GPU deployment...
echo This will skip all GPU detection and force GPU mode.
echo.

REM Execute the PowerShell script
powershell -ExecutionPolicy Bypass -File "start-force-gpu.ps1"

echo.
echo Force GPU deployment script completed.
pause 