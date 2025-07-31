@echo off
echo ========================================
echo Getting Host Machine IP Address
echo ========================================
echo.

echo Docker network IP (internal):
ipconfig | findstr "172.21.32.1"
echo.

echo Host machine IP addresses:
ipconfig | findstr "IPv4"
echo.

echo External network IPs (non-Docker):
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4"') do (
    echo %%a
)
echo.

echo Testing common local IPs:
echo Testing 192.168.x.x range...
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4" ^| findstr "192.168"') do (
    echo Found: %%a
    curl -s -I http://%%a:5000 2>nul | findstr "HTTP" || echo Failed to connect to %%a:5000
)
echo.

echo Testing 10.x.x.x range...
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr "IPv4" ^| findstr "10."') do (
    echo Found: %%a
    curl -s -I http://%%a:5000 2>nul | findstr "HTTP" || echo Failed to connect to %%a:5000
)
echo.

echo.
echo Try accessing the web portal using these IPs:
echo - http://[HOST_IP]:5000
echo.
echo If none work, restart with Windows deployment config:
echo restart-windows-deployment.bat
echo.

pause 