Write-Host "========================================" -ForegroundColor Green
Write-Host "Deep Diagnosis - Root Cause Analysis" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check Docker status
Write-Host "1. Docker Status:" -ForegroundColor Yellow
$dockerInfo = docker info 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   PASS: Docker is running" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Docker is not running or accessible" -ForegroundColor Red
    exit 1
}

# Check Docker networks
Write-Host ""
Write-Host "2. Docker Networks:" -ForegroundColor Yellow
$networks = docker network ls 2>&1
Write-Host $networks -ForegroundColor Gray

# Check if langchain_search_service network exists
$networkExists = docker network ls --format "{{.Name}}" | Select-String "langchain_search_service"
if ($networkExists) {
    Write-Host "   PASS: langchain_search_service network exists" -ForegroundColor Green
} else {
    Write-Host "   FAIL: langchain_search_service network missing" -ForegroundColor Red
}

# Check container states in detail
Write-Host ""
Write-Host "3. Container States (Detailed):" -ForegroundColor Yellow
$containers = docker ps -a --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}\t{{.Image}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Check which containers are actually running
$runningContainers = docker ps --format "{{.Names}}" 2>$null
Write-Host ""
Write-Host "   Running containers: $($runningContainers -join ', ')" -ForegroundColor Gray

# Check container resource usage
Write-Host ""
Write-Host "4. Container Resource Usage:" -ForegroundColor Yellow
$resourceUsage = docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}" 2>&1
Write-Host $resourceUsage -ForegroundColor Gray

# Check container logs for specific errors
Write-Host ""
Write-Host "5. Container Logs Analysis:" -ForegroundColor Yellow

# Check LangChain Service logs
Write-Host "   LangChain Service logs (last 20 lines):" -ForegroundColor Gray
try {
    $langchainLogs = docker logs --tail 20 langchain_search_service-langchain-service-1 2>&1
    if ($langchainLogs) {
        Write-Host $langchainLogs -ForegroundColor Gray
    } else {
        Write-Host "   No logs available" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Cannot access LangChain logs" -ForegroundColor Red
}

Write-Host ""
Write-Host "   CHAP2 API logs (last 20 lines):" -ForegroundColor Gray
try {
    $apiLogs = docker logs --tail 20 langchain_search_service-chap2-api-1 2>&1
    if ($apiLogs) {
        Write-Host $apiLogs -ForegroundColor Gray
    } else {
        Write-Host "   No logs available" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Cannot access API logs" -ForegroundColor Red
}

Write-Host ""
Write-Host "   Web Portal logs (last 20 lines):" -ForegroundColor Gray
try {
    $webLogs = docker logs --tail 20 langchain_search_service-chap2-webportal-1 2>&1
    if ($webLogs) {
        Write-Host $webLogs -ForegroundColor Gray
    } else {
        Write-Host "   No logs available" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Cannot access Web Portal logs" -ForegroundColor Red
}

# Check if services are listening on ports
Write-Host ""
Write-Host "6. Port Listening Check:" -ForegroundColor Yellow

Write-Host "   Checking what's listening on port 5001 (CHAP2 API):" -ForegroundColor Gray
$port5001 = netstat -an | Select-String ":5001"
if ($port5001) {
    Write-Host "   Port 5001 is in use: $port5001" -ForegroundColor Yellow
} else {
    Write-Host "   Port 5001 is not in use" -ForegroundColor Red
}

Write-Host "   Checking what's listening on port 8000 (LangChain):" -ForegroundColor Gray
$port8000 = netstat -an | Select-String ":8000"
if ($port8000) {
    Write-Host "   Port 8000 is in use: $port8000" -ForegroundColor Yellow
} else {
    Write-Host "   Port 8000 is not in use" -ForegroundColor Red
}

Write-Host "   Checking what's listening on port 5000 (Web Portal):" -ForegroundColor Gray
$port5000 = netstat -an | Select-String ":5000"
if ($port5000) {
    Write-Host "   Port 5000 is in use: $port5000" -ForegroundColor Yellow
} else {
    Write-Host "   Port 5000 is not in use" -ForegroundColor Red
}

# Check container internal processes
Write-Host ""
Write-Host "7. Container Internal Processes:" -ForegroundColor Yellow

Write-Host "   LangChain Service processes:" -ForegroundColor Gray
try {
    $langchainProcesses = docker exec langchain_search_service-langchain-service-1 ps aux 2>$null
    if ($langchainProcesses) {
        Write-Host $langchainProcesses -ForegroundColor Gray
    } else {
        Write-Host "   Cannot check LangChain processes" -ForegroundColor Red
    }
} catch {
    Write-Host "   Cannot access LangChain container" -ForegroundColor Red
}

Write-Host "   Web Portal processes:" -ForegroundColor Gray
try {
    $webProcesses = docker exec langchain_search_service-chap2-webportal-1 ps aux 2>$null
    if ($webProcesses) {
        Write-Host $webProcesses -ForegroundColor Gray
    } else {
        Write-Host "   Cannot check Web Portal processes" -ForegroundColor Red
    }
} catch {
    Write-Host "   Cannot access Web Portal container" -ForegroundColor Red
}

# Check if containers can reach each other
Write-Host ""
Write-Host "8. Container-to-Container Connectivity:" -ForegroundColor Yellow

Write-Host "   Testing if containers can ping each other:" -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 1 qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Qdrant" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

# Check Docker Compose configuration
Write-Host ""
Write-Host "9. Docker Compose Configuration:" -ForegroundColor Yellow
Write-Host "   Checking docker-compose.yml syntax:" -ForegroundColor Gray
$composeCheck = docker-compose config 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   PASS: Docker Compose configuration is valid" -ForegroundColor Green
} else {
    Write-Host "   FAIL: Docker Compose configuration has errors" -ForegroundColor Red
    Write-Host $composeCheck -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deep diagnosis complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Based on the results, the most likely issues are:" -ForegroundColor Yellow
Write-Host "1. Containers not starting properly (check logs)" -ForegroundColor White
Write-Host "2. Port conflicts (check netstat output)" -ForegroundColor White
Write-Host "3. Network configuration issues" -ForegroundColor White
Write-Host "4. Service configuration problems" -ForegroundColor White
Write-Host "5. Docker Desktop issues" -ForegroundColor White 