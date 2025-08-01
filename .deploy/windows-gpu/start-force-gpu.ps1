#Requires -Version 5.1

Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Force GPU Deployment" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Force GPU mode - skip all detection
$script:UseGpu = $true
Write-Host "FORCE: GPU mode enabled - skipping all detection" -ForegroundColor Yellow

# Step 1: Check prerequisites
Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Yellow

# Check if Docker is installed and running
try {
    $dockerVersion = docker --version
    Write-Host "   Docker version: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Docker is running
try {
    $dockerInfo = docker info 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Docker is running" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   FAIL: Cannot check Docker status" -ForegroundColor Red
    exit 1
}

# Check NVIDIA Container Toolkit
Write-Host "   Checking NVIDIA Container Toolkit..." -ForegroundColor Gray
try {
    $gpuTest = docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: NVIDIA Container Toolkit working" -ForegroundColor Green
    } else {
        Write-Host "   WARNING: NVIDIA Container Toolkit not working, but continuing..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "   WARNING: Could not test NVIDIA Container Toolkit" -ForegroundColor Yellow
}

# Step 2: Stop existing containers
Write-Host "Step 2: Stopping existing containers..." -ForegroundColor Yellow
try {
    docker-compose down
    Start-Sleep -Seconds 5
    Write-Host "   PASS: Existing containers stopped" -ForegroundColor Green
} catch {
    Write-Host "   WARNING: Could not stop existing containers" -ForegroundColor Yellow
}

# Step 3: Build and start with GPU
Write-Host "Step 3: Building and starting containers with GPU..." -ForegroundColor Yellow
try {
    Write-Host "   Building containers with GPU support..." -ForegroundColor Gray
    docker-compose build --no-cache
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   FAIL: Container build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   PASS: Containers built successfully" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not build containers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start all containers
Write-Host "   Starting all containers with GPU..." -ForegroundColor Gray
try {
    docker-compose up -d
    Start-Sleep -Seconds 30
    
    # Check if all containers are running
    $allContainers = docker ps --filter "name=chap2" --format "{{.Names}}\t{{.Status}}"
    Write-Host "   Container status:" -ForegroundColor Gray
    Write-Host $allContainers -ForegroundColor Gray
    
    # Verify all containers are up
    $runningContainers = docker ps --filter "name=chap2" --filter "status=running" --format "{{.Names}}" | Measure-Object -Line
    $totalContainers = docker ps --filter "name=chap2" --format "{{.Names}}" | Measure-Object -Line
    
    if ($runningContainers.Lines -eq $totalContainers.Lines) {
        Write-Host "   PASS: All containers started successfully with GPU" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Some containers failed to start" -ForegroundColor Red
        docker-compose logs
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start containers: $($_.Exception.Message)" -ForegroundColor Red
    docker-compose logs
    exit 1
}

# Step 4: Wait for services to be ready
Write-Host "Step 4: Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Step 5: Test services
Write-Host "Step 5: Testing services..." -ForegroundColor Yellow

# Test Web Portal
Write-Host "   Testing Web Portal..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 10
    Write-Host "   PASS: Web Portal accessible (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test API
Write-Host "   Testing API..." -ForegroundColor Gray
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/health/ping" -TimeoutSec 10
    Write-Host "   PASS: API accessible (Status: $($apiResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: API not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test LangChain Service
Write-Host "   Testing LangChain Service..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Qdrant
Write-Host "   Testing Qdrant..." -ForegroundColor Gray
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    Write-Host "   PASS: Qdrant accessible (Status: $($qdrantResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Qdrant not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Get server IP
Write-Host "   Getting server IP address..." -ForegroundColor Gray
$serverIP = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -notlike "169.254.*" -and $_.IPAddress -notlike "127.*"} | Select-Object -First 1).IPAddress
Write-Host "   Server IP: $serverIP" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "FORCE GPU Deployment complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- Web Portal: http://$serverIP`:5000" -ForegroundColor White
Write-Host "- API: http://$serverIP`:5001" -ForegroundColor White
Write-Host "- LangChain Service: http://$serverIP`:8000" -ForegroundColor White
Write-Host "- Qdrant: http://$serverIP`:6333" -ForegroundColor White

Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "- Open http://$serverIP`:5000 in your browser" -ForegroundColor White
Write-Host "- Use the search functionality to test the system" -ForegroundColor White
Write-Host "- Check logs with: docker-compose logs -f" -ForegroundColor White
Write-Host "- Stop services with: docker-compose down" -ForegroundColor White

Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "- If services are not accessible, try: docker-compose restart" -ForegroundColor White
Write-Host "- If search doesn't work, check container logs" -ForegroundColor White
Write-Host "- For GPU issues, restart Docker Desktop" -ForegroundColor White
Write-Host "- For container issues, check: docker-compose logs [service-name]" -ForegroundColor White 