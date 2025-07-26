Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Network Connectivity Diagnostic" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Check container status
Write-Host "Step 1: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 2: Check Docker networks
Write-Host "Step 2: Checking Docker networks..." -ForegroundColor Yellow
$networks = docker network ls --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}" 2>&1
Write-Host $networks -ForegroundColor Gray

# Step 3: Check which network containers are on
Write-Host "Step 3: Checking container network assignments..." -ForegroundColor Yellow
$containerNetworks = docker inspect langchain_search_service-langchain-service-1 --format "{{range .NetworkSettings.Networks}}{{.NetworkID}} {{.IPAddress}}{{end}}" 2>$null
Write-Host "   LangChain Service Network: $containerNetworks" -ForegroundColor Gray

$containerNetworks = docker inspect langchain_search_service-qdrant-1 --format "{{range .NetworkSettings.Networks}}{{.NetworkID}} {{.IPAddress}}{{end}}" 2>$null
Write-Host "   Qdrant Network: $containerNetworks" -ForegroundColor Gray

$containerNetworks = docker inspect langchain_search_service-ollama-1 --format "{{range .NetworkSettings.Networks}}{{.NetworkID}} {{.IPAddress}}{{end}}" 2>$null
Write-Host "   Ollama Network: $containerNetworks" -ForegroundColor Gray

# Step 4: Check if containers can resolve DNS
Write-Host "Step 4: Testing DNS resolution..." -ForegroundColor Yellow

Write-Host "   Testing LangChain DNS resolution..." -ForegroundColor Gray
try {
    $dnsTest = docker exec langchain_search_service-langchain-service-1 nslookup qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can resolve qdrant DNS" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot resolve qdrant DNS" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test DNS resolution" -ForegroundColor Red
}

# Step 5: Check if curl is available in containers
Write-Host "Step 5: Checking if curl is available..." -ForegroundColor Yellow

Write-Host "   Testing curl in LangChain container..." -ForegroundColor Gray
try {
    $curlTest = docker exec langchain_search_service-langchain-service-1 which curl 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: curl is available in LangChain container" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: curl not available in LangChain container" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test curl availability" -ForegroundColor Red
}

Write-Host "   Testing curl in Web Portal container..." -ForegroundColor Gray
try {
    $curlTest = docker exec langchain_search_service-chap2-webportal-1 which curl 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: curl is available in Web Portal container" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: curl not available in Web Portal container" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test curl availability" -ForegroundColor Red
}

# Step 6: Test with wget if curl is not available
Write-Host "Step 6: Testing with wget as fallback..." -ForegroundColor Yellow

Write-Host "   Testing wget in LangChain container..." -ForegroundColor Gray
try {
    $wgetTest = docker exec langchain_search_service-langchain-service-1 which wget 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: wget is available in LangChain container" -ForegroundColor Green
        
        # Test connectivity with wget
        $connectivityTest = docker exec langchain_search_service-langchain-service-1 wget -q --spider http://qdrant:6333/collections 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: LangChain can reach Qdrant via wget" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: LangChain cannot reach Qdrant via wget" -ForegroundColor Red
        }
    } else {
        Write-Host "   FAIL: wget not available in LangChain container" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test wget" -ForegroundColor Red
}

# Step 7: Check container logs for network errors
Write-Host "Step 7: Checking container logs for network errors..." -ForegroundColor Yellow

Write-Host "   LangChain Service logs (last 10 lines):" -ForegroundColor Gray
try {
    $langchainLogs = docker logs --tail 10 langchain_search_service-langchain-service-1 2>&1
    Write-Host $langchainLogs -ForegroundColor Gray
} catch {
    Write-Host "   Cannot get LangChain logs" -ForegroundColor Red
}

# Step 8: Test direct IP connectivity
Write-Host "Step 8: Testing direct IP connectivity..." -ForegroundColor Yellow

Write-Host "   Getting container IPs..." -ForegroundColor Gray
try {
    $qdrantIP = docker inspect langchain_search_service-qdrant-1 --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" 2>$null
    Write-Host "   Qdrant IP: $qdrantIP" -ForegroundColor Gray
    
    if ($qdrantIP) {
        $ipTest = docker exec langchain_search_service-langchain-service-1 curl -s http://$qdrantIP`:6333/collections 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: LangChain can reach Qdrant by IP" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: LangChain cannot reach Qdrant by IP" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "   FAIL: Cannot test IP connectivity" -ForegroundColor Red
}

# Step 9: Check Docker network inspect
Write-Host "Step 9: Inspecting Docker network..." -ForegroundColor Yellow
try {
    $networkInfo = docker network inspect langchain_search_service_default 2>&1
    Write-Host "   Network info:" -ForegroundColor Gray
    Write-Host $networkInfo -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: Cannot inspect network" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Diagnostic completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Troubleshooting suggestions:" -ForegroundColor Yellow
Write-Host "1. If containers are on different networks, restart with: docker-compose -f docker-compose.gpu.yml down && docker-compose -f docker-compose.gpu.yml up -d" -ForegroundColor White
Write-Host "2. If curl/wget not available, install them in the Dockerfile" -ForegroundColor White
Write-Host "3. If DNS resolution fails, check Docker DNS settings" -ForegroundColor White
Write-Host "4. If IP connectivity works but DNS doesn't, it's a DNS issue" -ForegroundColor White
Write-Host "5. Check Docker Desktop network settings" -ForegroundColor White 