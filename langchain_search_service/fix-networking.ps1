Write-Host "========================================" -ForegroundColor Green
Write-Host "Fixing Docker Networking Issues" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Stop all containers and clean up
Write-Host "Step 1: Stopping all containers and cleaning up..." -ForegroundColor Yellow
docker-compose down
Start-Sleep -Seconds 5

# Force remove any remaining containers
$remainingContainers = docker ps -aq --filter "name=langchain_search_service" 2>$null
if ($remainingContainers) {
    Write-Host "   Force removing remaining containers..." -ForegroundColor Red
    docker rm -f $remainingContainers 2>$null
}

# Step 2: Remove and recreate the network
Write-Host "Step 2: Recreating Docker network..." -ForegroundColor Yellow
docker network rm langchain_search_service_default 2>$null
Start-Sleep -Seconds 2
docker network create langchain_search_service_default 2>$null

# Step 3: Check network configuration
Write-Host "Step 3: Checking network configuration..." -ForegroundColor Yellow
$networks = docker network ls --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}" 2>&1
Write-Host $networks -ForegroundColor Gray

# Step 4: Start containers with explicit network
Write-Host "Step 4: Starting containers with explicit network..." -ForegroundColor Yellow
docker-compose up -d

# Step 5: Wait for containers to be ready
Write-Host "Step 5: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Step 6: Check container status
Write-Host "Step 6: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 7: Check network connectivity
Write-Host "Step 7: Testing network connectivity..." -ForegroundColor Yellow

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

Write-Host "   Testing Web Portal -> LangChain ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-chap2-webportal-1 ping -c 3 langchain-service 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Web Portal can ping LangChain" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal cannot ping LangChain" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

# Step 8: Check DNS resolution
Write-Host "Step 8: Testing DNS resolution..." -ForegroundColor Yellow

Write-Host "   Testing DNS resolution in LangChain container..." -ForegroundColor Gray
try {
    $dnsTest = docker exec langchain_search_service-langchain-service-1 nslookup qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: DNS resolution works in LangChain" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: DNS resolution failed in LangChain" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test DNS" -ForegroundColor Red
}

# Step 9: Check network interfaces
Write-Host "Step 9: Checking network interfaces..." -ForegroundColor Yellow

Write-Host "   LangChain container network interfaces:" -ForegroundColor Gray
try {
    $interfaces = docker exec langchain_search_service-langchain-service-1 ip addr 2>$null
    Write-Host $interfaces -ForegroundColor Gray
} catch {
    Write-Host "   Cannot check network interfaces" -ForegroundColor Red
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

# Step 11: Check Docker network inspect
Write-Host "Step 11: Inspecting Docker network..." -ForegroundColor Yellow
try {
    $networkInfo = docker network inspect langchain_search_service_default 2>&1
    Write-Host $networkInfo -ForegroundColor Gray
} catch {
    Write-Host "   Cannot inspect network" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Network fix attempt complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If networking issues persist, try these manual steps:" -ForegroundColor Yellow
Write-Host "1. Restart Docker Desktop completely" -ForegroundColor White
Write-Host "2. Run: docker system prune -f" -ForegroundColor White
Write-Host "3. Run: docker network prune -f" -ForegroundColor White
Write-Host "4. Check Windows Firewall settings" -ForegroundColor White
Write-Host "5. Try using host networking: docker-compose up -d --network host" -ForegroundColor White 