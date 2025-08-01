@echo off
echo ========================================
echo Stopping CHAP2 Windows GPU Deployment
echo ========================================
echo.

echo Stopping all containers...
docker-compose down
echo.

echo Checking if containers are stopped:
docker ps
echo.

echo Deployment stopped successfully.
pause 