Write-Host "========================================" -ForegroundColor Green
Write-Host "Diagnosing and Fixing Container Issues" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Check Docker status
Write-Host "Step 1: Checking Docker status..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "   Docker version: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Docker not available" -ForegroundColor Red
    exit 1
}

# Step 2: Check if containers are running
Write-Host "Step 2: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 3: Check if containers exist but not running
Write-Host "Step 3: Checking for stopped containers..." -ForegroundColor Yellow
$stoppedContainers = docker ps -a --filter "status=exited" --filter "name=langchain_search_service" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
if ($stoppedContainers -and $stoppedContainers -notmatch "NAMES") {
    Write-Host $stoppedContainers -ForegroundColor Red
    Write-Host "   Found stopped containers - will restart them" -ForegroundColor Yellow
} else {
    Write-Host "   No stopped containers found" -ForegroundColor Green
}

# Step 4: Check container logs for errors
Write-Host "Step 4: Checking container logs for errors..." -ForegroundColor Yellow

Write-Host "   CHAP2 API logs:" -ForegroundColor Gray
try {
    $apiLogs = docker logs langchain_search_service-chap2-api-1 --tail 20 2>&1
    Write-Host $apiLogs -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get API logs" -ForegroundColor Red
}

Write-Host "   Web Portal logs:" -ForegroundColor Gray
try {
    $webLogs = docker logs langchain_search_service-chap2-webportal-1 --tail 20 2>&1
    Write-Host $webLogs -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get Web Portal logs" -ForegroundColor Red
}

Write-Host "   LangChain Service logs:" -ForegroundColor Gray
try {
    $langchainLogs = docker logs langchain_search_service-langchain-service-1 --tail 20 2>&1
    Write-Host $langchainLogs -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get LangChain logs" -ForegroundColor Red
}

# Step 5: Check port bindings
Write-Host "Step 5: Checking port bindings..." -ForegroundColor Yellow
$portBindings = docker ps --format "table {{.Names}}\t{{.Ports}}" 2>&1
Write-Host $portBindings -ForegroundColor Gray

# Step 6: Test if ports are actually listening
Write-Host "Step 6: Testing port accessibility..." -ForegroundColor Yellow

Write-Host "   Testing port 5001 (CHAP2 API)..." -ForegroundColor Gray
try {
    $apiTest = Test-NetConnection -ComputerName localhost -Port 5001 -InformationLevel Quiet
    if ($apiTest) {
        Write-Host "   PASS: Port 5001 is listening" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Port 5001 is not listening" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test port 5001" -ForegroundColor Red
}

Write-Host "   Testing port 5000 (Web Portal)..." -ForegroundColor Gray
try {
    $webTest = Test-NetConnection -ComputerName localhost -Port 5000 -InformationLevel Quiet
    if ($webTest) {
        Write-Host "   PASS: Port 5000 is listening" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Port 5000 is not listening" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test port 5000" -ForegroundColor Red
}

Write-Host "   Testing port 8000 (LangChain Service)..." -ForegroundColor Gray
try {
    $langchainTest = Test-NetConnection -ComputerName localhost -Port 8000 -InformationLevel Quiet
    if ($langchainTest) {
        Write-Host "   PASS: Port 8000 is listening" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Port 8000 is not listening" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test port 8000" -ForegroundColor Red
}

# Step 7: Force rebuild and restart if needed
Write-Host "Step 7: Force rebuilding and restarting containers..." -ForegroundColor Yellow

Write-Host "   Stopping all containers..." -ForegroundColor Gray
docker-compose down
Start-Sleep -Seconds 5

Write-Host "   Rebuilding containers..." -ForegroundColor Gray
docker-compose build --no-cache

Write-Host "   Starting containers..." -ForegroundColor Gray
docker-compose up -d

# Step 8: Wait for containers to be ready
Write-Host "Step 8: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Step 9: Check container status again
Write-Host "Step 9: Checking container status after restart..." -ForegroundColor Yellow
$containersAfter = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containersAfter -ForegroundColor Gray

# Step 10: Test HTTP endpoints
Write-Host "Step 10: Testing HTTP endpoints..." -ForegroundColor Yellow

Write-Host "   Testing CHAP2 API (localhost:5001)..." -ForegroundColor Gray
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 10
    Write-Host "   PASS: CHAP2 API accessible (Status: $($apiResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: CHAP2 API not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal (localhost:5000)..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 10
    Write-Host "   PASS: Web Portal accessible (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service (localhost:8000)..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 11: Check for specific .NET startup issues
Write-Host "Step 11: Checking for .NET startup issues..." -ForegroundColor Yellow

Write-Host "   Checking CHAP2 API container processes..." -ForegroundColor Gray
try {
    $apiProcesses = docker exec langchain_search_service-chap2-api-1 ps aux 2>$null
    Write-Host $apiProcesses -ForegroundColor Gray
} catch {
    Write-Host "   Cannot check API processes" -ForegroundColor Red
}

Write-Host "   Checking Web Portal container processes..." -ForegroundColor Gray
try {
    $webProcesses = docker exec langchain_search_service-chap2-webportal-1 ps aux 2>$null
    Write-Host $webProcesses -ForegroundColor Gray
} catch {
    Write-Host "   Cannot check Web Portal processes" -ForegroundColor Red
}

# Step 12: Check environment variables
Write-Host "Step 12: Checking environment variables..." -ForegroundColor Yellow

Write-Host "   CHAP2 API environment:" -ForegroundColor Gray
try {
    $apiEnv = docker exec langchain_search_service-chap2-api-1 env 2>$null
    Write-Host $apiEnv -ForegroundColor Gray
} catch {
    Write-Host "   Cannot check API environment" -ForegroundColor Red
}

Write-Host "   Web Portal environment:" -ForegroundColor Gray
try {
    $webEnv = docker exec langchain_search_service-chap2-webportal-1 env 2>$null
    Write-Host $webEnv -ForegroundColor Gray
} catch {
    Write-Host "   Cannot check Web Portal environment" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Diagnosis complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If containers are still not accessible, try:" -ForegroundColor Yellow
Write-Host "1. Check Windows Firewall settings" -ForegroundColor White
Write-Host "2. Restart Docker Desktop completely" -ForegroundColor White
Write-Host "3. Check if ports 5000, 5001, 8000 are already in use" -ForegroundColor White
Write-Host "4. Try different ports in docker-compose.yml" -ForegroundColor White
Write-Host "5. Check Docker Desktop settings for port exposure" -ForegroundColor White 