Write-Host "========================================" -ForegroundColor Green
Write-Host "Checking CHAP2 API Status" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Check if API container is running
Write-Host "Step 1: Checking API container status..." -ForegroundColor Yellow
$apiContainer = docker ps --filter "name=chap2-api" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $apiContainer -ForegroundColor Gray

# Step 2: Check if API container exists but stopped
Write-Host "Step 2: Checking for stopped API container..." -ForegroundColor Yellow
$stoppedApiContainer = docker ps -a --filter "name=chap2-api" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $stoppedApiContainer -ForegroundColor Gray

# Step 3: Get API container logs
Write-Host "Step 3: Getting API container logs..." -ForegroundColor Yellow
try {
    $apiLogs = docker logs langchain_search_service-chap2-api-1 --tail 50 2>&1
    Write-Host "   API Container Logs:" -ForegroundColor Gray
    Write-Host $apiLogs -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get API logs: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Check if port 5001 is in use
Write-Host "Step 4: Checking if port 5001 is in use..." -ForegroundColor Yellow
try {
    $portCheck = netstat -an | findstr ":5001"
    if ($portCheck) {
        Write-Host "   Port 5001 is in use:" -ForegroundColor Red
        Write-Host $portCheck -ForegroundColor Gray
    } else {
        Write-Host "   Port 5001 is not in use" -ForegroundColor Green
    }
} catch {
    Write-Host "   Cannot check port usage" -ForegroundColor Red
}

# Step 5: Check container resource usage
Write-Host "Step 5: Checking container resource usage..." -ForegroundColor Yellow
try {
    $containerStats = docker stats langchain_search_service-chap2-api-1 --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}" 2>&1
    Write-Host $containerStats -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get container stats" -ForegroundColor Red
}

# Step 6: Try to restart the API container
Write-Host "Step 6: Attempting to restart API container..." -ForegroundColor Yellow
try {
    docker restart langchain_search_service-chap2-api-1
    Write-Host "   API container restart initiated" -ForegroundColor Green
    Start-Sleep -Seconds 10
    
    # Check status after restart
    $apiContainerAfter = docker ps --filter "name=chap2-api" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
    Write-Host "   API container status after restart:" -ForegroundColor Gray
    Write-Host $apiContainerAfter -ForegroundColor Gray
} catch {
    Write-Host "   Cannot restart API container: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Test API endpoint after restart
Write-Host "Step 7: Testing API endpoint after restart..." -ForegroundColor Yellow
Start-Sleep -Seconds 5
try {
    $apiTest = Invoke-WebRequest -Uri "http://localhost:5001/health/ping" -TimeoutSec 10
    Write-Host "   PASS: API accessible after restart (Status: $($apiTest.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: API still not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 8: Check all containers status
Write-Host "Step 8: Checking all containers status..." -ForegroundColor Yellow
$allContainers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $allContainers -ForegroundColor Gray

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "API status check complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If API is still down, try:" -ForegroundColor Yellow
Write-Host "1. Check Docker Desktop is running" -ForegroundColor White
Write-Host "2. Restart all containers: docker-compose restart" -ForegroundColor White
Write-Host "3. Rebuild API container: docker-compose build chap2-api" -ForegroundColor White
Write-Host "4. Check Windows Firewall settings" -ForegroundColor White
Write-Host "5. Check if .NET runtime is available in container" -ForegroundColor White 