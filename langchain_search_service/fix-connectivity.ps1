Write-Host "========================================" -ForegroundColor Green
Write-Host "Fixing Service Connectivity Issues" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Stop all containers
Write-Host "Step 1: Stopping all containers..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 5

# Step 2: Check if containers are actually stopped
Write-Host "Step 2: Verifying containers are stopped..." -ForegroundColor Yellow
$runningContainers = docker ps --format "{{.Names}}" 2>$null
if ($runningContainers -match "langchain_search_service") {
    Write-Host "   Force stopping remaining containers..." -ForegroundColor Red
    docker stop $(docker ps -q --filter "name=langchain_search_service") 2>$null
    docker rm $(docker ps -aq --filter "name=langchain_search_service") 2>$null
}

# Step 3: Start containers with proper networking
Write-Host "Step 3: Starting containers with proper networking..." -ForegroundColor Yellow
docker-compose up -d

# Step 4: Wait for containers to be ready
Write-Host "Step 4: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Step 5: Check container status
Write-Host "Step 5: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 6: Check if all expected containers are running
$expectedContainers = @("qdrant", "ollama", "langchain-service", "chap2-api", "chap2-webportal")
$missingContainers = @()

foreach ($container in $expectedContainers) {
    if ($containers -notmatch $container) {
        $missingContainers += $container
    }
}

if ($missingContainers.Count -gt 0) {
    Write-Host "   Missing containers: $($missingContainers -join ', ')" -ForegroundColor Red
    Write-Host "   Attempting to restart missing containers..." -ForegroundColor Yellow
    docker-compose up -d $($missingContainers -join ' ')
    Start-Sleep -Seconds 10
} else {
    Write-Host "   All expected containers are running" -ForegroundColor Green
}

# Step 7: Check container logs for startup issues
Write-Host "Step 7: Checking container logs for startup issues..." -ForegroundColor Yellow

Write-Host "   LangChain Service logs:" -ForegroundColor Gray
try {
    docker logs --tail 10 langchain_search_service-langchain-service-1 2>$null
} catch {
    Write-Host "   Cannot access LangChain logs" -ForegroundColor Red
}

Write-Host "   CHAP2 API logs:" -ForegroundColor Gray
try {
    docker logs --tail 10 langchain_search_service-chap2-api-1 2>$null
} catch {
    Write-Host "   Cannot access API logs" -ForegroundColor Red
}

Write-Host "   Web Portal logs:" -ForegroundColor Gray
try {
    docker logs --tail 10 langchain_search_service-chap2-webportal-1 2>$null
} catch {
    Write-Host "   Cannot access Web Portal logs" -ForegroundColor Red
}

# Step 8: Test internal connectivity
Write-Host "Step 8: Testing internal connectivity..." -ForegroundColor Yellow

Write-Host "   Testing LangChain -> Qdrant..." -ForegroundColor Gray
try {
    $qdrantTest = docker exec langchain_search_service-langchain-service-1 curl -s http://qdrant:6333/collections 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Qdrant" -ForegroundColor Red
        Write-Host "   Trying alternative Qdrant URL..." -ForegroundColor Yellow
        docker exec langchain_search_service-langchain-service-1 curl -s http://qdrant:6333/health 2>$null
    }
} catch {
    Write-Host "   FAIL: Cannot test Qdrant connectivity" -ForegroundColor Red
}

Write-Host "   Testing LangChain -> Ollama..." -ForegroundColor Gray
try {
    $ollamaTest = docker exec langchain_search_service-langchain-service-1 curl -s http://ollama:11434/api/tags 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Ollama" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Ollama" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test Ollama connectivity" -ForegroundColor Red
}

Write-Host "   Testing Web Portal -> LangChain..." -ForegroundColor Gray
try {
    $langchainTest = docker exec langchain_search_service-chap2-webportal-1 curl -s http://langchain-service:8000/health 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Web Portal can reach LangChain" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal cannot reach LangChain" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test LangChain connectivity" -ForegroundColor Red
}

# Step 9: Test external endpoints
Write-Host "Step 9: Testing external endpoints..." -ForegroundColor Yellow

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

# Step 10: Check environment variables
Write-Host "Step 10: Checking environment variables..." -ForegroundColor Yellow

Write-Host "   LangChain Service environment:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-langchain-service-1 env | Select-String "QDRANT_URL|OLLAMA_URL" 2>$null
} catch {
    Write-Host "   Cannot check LangChain environment" -ForegroundColor Red
}

Write-Host "   Web Portal environment:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-chap2-webportal-1 env | Select-String "LangChainService|ApiService" 2>$null
} catch {
    Write-Host "   Cannot check Web Portal environment" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Connectivity fix attempt complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If issues persist, try these manual steps:" -ForegroundColor Yellow
Write-Host "1. Check Docker Desktop is running" -ForegroundColor White
Write-Host "2. Restart Docker Desktop" -ForegroundColor White
Write-Host "3. Run: docker system prune -f" -ForegroundColor White
Write-Host "4. Run: docker-compose down && docker-compose up -d" -ForegroundColor White
Write-Host "5. Check firewall settings" -ForegroundColor White 