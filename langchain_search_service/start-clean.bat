@echo off
setlocal enabledelayedexpansion

echo ========================================
echo CHAP2 LangChain Service - Clean Start
echo ========================================

REM Check if PowerShell is available
powershell -Command "Write-Host 'PowerShell is available'" >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: PowerShell is not available on this system.
    pause
    exit /b 1
)

REM Check if the PowerShell script exists
if not exist "start-clean.ps1" (
    echo ERROR: PowerShell script not found.
    echo Please ensure you are in the correct directory.
    pause
    exit /b 1
)

echo Starting clean deployment...
echo.

REM Execute the PowerShell script
powershell -ExecutionPolicy Bypass -File "start-clean.ps1"

REM Check the exit code
if %errorlevel% neq 0 (
    echo.
    echo Deployment failed with exit code: %errorlevel%
    pause
    exit /b %errorlevel%
)

echo.
echo Deployment completed successfully.
pause 