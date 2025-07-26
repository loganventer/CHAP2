Write-Host "========================================" -ForegroundColor Green
Write-Host "Fixing Docker Compose Network Issue" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Stop all containers
Write-Host "Step 1: Stopping all containers..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 5

# Step 2: Remove the existing network that wasn't created by compose
Write-Host "Step 2: Removing existing network..." -ForegroundColor Yellow
docker network rm langchain_search_service_default 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   PASS: Removed existing network" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Could not remove network (may not exist)" -ForegroundColor Red
}

# Step 3: Clean up any orphaned containers
Write-Host "Step 3: Cleaning up orphaned containers..." -ForegroundColor Yellow
$orphanedContainers = docker ps -aq --filter "name=langchain_search_service" 2>$null
if ($orphanedContainers) {
    docker rm -f $orphanedContainers 2>$null
    Write-Host "   Removed orphaned containers" -ForegroundColor Green
} else {
    Write-Host "   No orphaned containers found" -ForegroundColor Green
}

# Step 4: Check current networks
Write-Host "Step 4: Checking current networks..." -ForegroundColor Yellow
$networks = docker network ls --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}" 2>&1
Write-Host $networks -ForegroundColor Gray

# Step 5: Start containers with compose (this will create the network properly)
Write-Host "Step 5: Starting containers with Docker Compose..." -ForegroundColor Yellow
docker-compose up -d

# Step 6: Wait for containers to be ready
Write-Host "Step 6: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Step 7: Verify network was created by compose
Write-Host "Step 7: Verifying network creation..." -ForegroundColor Yellow
$composeNetworks = docker network ls --format "{{.Name}}" | Select-String "langchain_search_service"
if ($composeNetworks) {
    Write-Host "   PASS: Network created by Docker Compose" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Network not created by Docker Compose" -ForegroundColor Red
}

# Step 8: Check container status
Write-Host "Step 8: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 9: Test network connectivity
Write-Host "Step 9: Testing network connectivity..." -ForegroundColor Yellow

Write-Host "   Testing LangChain -> Qdrant ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Qdrant" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

Write-Host "   Testing LangChain -> Ollama ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 ollama 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Ollama" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Ollama" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

# Step 10: Test HTTP connectivity
Write-Host "Step 10: Testing HTTP connectivity..." -ForegroundColor Yellow

Write-Host "   Testing LangChain -> Qdrant HTTP..." -ForegroundColor Gray
try {
    $httpTest = docker exec langchain_search_service-langchain-service-1 curl -s http://qdrant:6333/collections 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Qdrant via HTTP" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Qdrant via HTTP" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test HTTP connectivity" -ForegroundColor Red
}

Write-Host "   Testing LangChain -> Ollama HTTP..." -ForegroundColor Gray
try {
    $httpTest = docker exec langchain_search_service-langchain-service-1 curl -s http://ollama:11434/api/tags 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Ollama via HTTP" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Ollama via HTTP" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test HTTP connectivity" -ForegroundColor Red
}

# Step 11: Check network details
Write-Host "Step 11: Checking network details..." -ForegroundColor Yellow
try {
    $networkDetails = docker network inspect langchain_search_service_default 2>&1
    Write-Host "   Network details:" -ForegroundColor Gray
    Write-Host $networkDetails -ForegroundColor Gray
} catch {
    Write-Host "   Cannot inspect network" -ForegroundColor Red
}

# Step 12: Test external endpoints
Write-Host "Step 12: Testing external endpoints..." -ForegroundColor Yellow

Write-Host "   Testing CHAP2 API (localhost:5001)..." -ForegroundColor Gray
try {
    $apiTest = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 10
    Write-Host "   PASS: CHAP2 API accessible (Status: $($apiTest.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: CHAP2 API not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service (localhost:8000)..." -ForegroundColor Gray
try {
    $langchainTest = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainTest.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Docker Compose network fix complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If issues persist, the problem might be:" -ForegroundColor Yellow
Write-Host "1. Docker Desktop networking issues" -ForegroundColor White
Write-Host "2. Windows Firewall blocking container communication" -ForegroundColor White
Write-Host "3. Docker Compose version compatibility" -ForegroundColor White
Write-Host "4. WSL2 networking issues (if using WSL2 backend)" -ForegroundColor White 