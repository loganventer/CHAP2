@echo off
echo ========================================
echo Running Windows Network Test
echo ========================================
echo.

echo Starting PowerShell network test...
powershell -ExecutionPolicy Bypass -File "fix-windows-network.ps1"

echo.
echo Network test completed.
pause 