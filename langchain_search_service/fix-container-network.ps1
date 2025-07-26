Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Container Network Fix" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Stop all containers
Write-Host "Step 1: Stopping all containers..." -ForegroundColor Yellow
try {
    docker-compose down
    Start-Sleep -Seconds 5
    Write-Host "   PASS: All containers stopped" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not stop containers: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 2: Remove the problematic network
Write-Host "Step 2: Removing problematic network..." -ForegroundColor Yellow
try {
    # Force remove the network
    docker network rm langchain_search_service_default 2>$null
    Start-Sleep -Seconds 3
    
    # Remove any orphaned containers
    $orphanedContainers = docker ps -aq --filter "name=langchain_search_service" 2>$null
    if ($orphanedContainers) {
        docker rm -f $orphanedContainers 2>$null
        Write-Host "   Removed orphaned containers" -ForegroundColor Green
    }
    
    Write-Host "   PASS: Network removed" -ForegroundColor Green
} catch {
    Write-Host "   WARNING: Could not remove network: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 3: Let Docker Compose create the network (don't create manually)
Write-Host "Step 3: Preparing for fresh network creation..." -ForegroundColor Yellow
Write-Host "   PASS: Ready for Docker Compose to create network" -ForegroundColor Green

# Step 4: Start containers with proper networking
Write-Host "Step 4: Starting containers with proper networking..." -ForegroundColor Yellow

# Remove the manually created network to let Docker Compose create it properly
Write-Host "   Removing manually created network..." -ForegroundColor Gray
try {
    docker network rm langchain_search_service_default 2>$null
    Start-Sleep -Seconds 2
} catch {
    # Network might not exist, that's okay
}

# Determine which compose file to use
$gpuAvailable = $false
try {
    $gpuTest = nvidia-smi 2>$null
    if ($LASTEXITCODE -eq 0) {
        $gpuAvailable = $true
    }
} catch {
    $gpuAvailable = $false
}

if ($gpuAvailable) {
    Write-Host "   Using GPU-enabled configuration..." -ForegroundColor Green
    $composeFile = "docker-compose.gpu.yml"
} else {
    Write-Host "   Using CPU-only configuration..." -ForegroundColor Yellow
    $composeFile = "docker-compose.yml"
}

try {
    # Start all containers at once to ensure proper network setup
    docker-compose -f $composeFile up -d
    Start-Sleep -Seconds 30
    
    Write-Host "   PASS: Containers started" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not start containers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 5: Wait for services to be ready
Write-Host "Step 5: Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Step 6: Test container connectivity
Write-Host "Step 6: Testing container connectivity..." -ForegroundColor Yellow

# Test LangChain -> Qdrant
Write-Host "   Testing LangChain -> Qdrant..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Qdrant" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test LangChain -> Qdrant ping" -ForegroundColor Red
}

# Test LangChain -> Ollama
Write-Host "   Testing LangChain -> Ollama..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 ollama 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Ollama" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Ollama" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test LangChain -> Ollama ping" -ForegroundColor Red
}

# Test Web Portal -> LangChain
Write-Host "   Testing Web Portal -> LangChain..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-chap2-webportal-1 ping -c 3 langchain-service 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Web Portal can ping LangChain" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal cannot ping LangChain" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test Web Portal -> LangChain ping" -ForegroundColor Red
}

# Step 7: Test search functionality
Write-Host "Step 7: Testing search functionality..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent" -Method POST -ContentType "application/json" -Body '{"query":"test","k":1}' -TimeoutSec 30
    Write-Host "   PASS: Search functionality working (Status: $($searchResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Search functionality not working: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 8: Show container status
Write-Host "Step 8: Container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Network fix completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "If containers still can't ping each other:" -ForegroundColor Yellow
Write-Host "1. Check Docker Desktop network settings" -ForegroundColor White
Write-Host "2. Restart Docker Desktop" -ForegroundColor White
Write-Host "3. Try: docker network prune" -ForegroundColor White
Write-Host "4. Check firewall settings" -ForegroundColor White 