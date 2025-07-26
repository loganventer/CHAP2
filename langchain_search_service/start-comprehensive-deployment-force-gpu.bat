@echo off
echo ========================================
echo CHAP2 Comprehensive Deployment - Force GPU
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
if not exist "start-comprehensive-deployment.ps1" (
    echo ERROR: start-comprehensive-deployment.ps1 not found
    echo Please ensure you are running this from the correct directory
    pause
    exit /b 1
)

echo Starting comprehensive deployment with FORCE GPU...
echo This will apply all fixes automatically and force GPU mode.
echo.

REM Execute the PowerShell script with ForceGpu parameter
powershell -ExecutionPolicy Bypass -File "start-comprehensive-deployment.ps1" -ForceGpu

echo.
echo Comprehensive deployment with force GPU completed.
pause 