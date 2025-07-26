# Fix Docker Build Issues Script
Write-Host "========================================" -ForegroundColor Green
Write-Host "Fixing Docker Build Issues" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Clean up Docker resources
Write-Host "Step 1: Cleaning up Docker resources..." -ForegroundColor Yellow

# Stop and remove containers
Write-Host "   Stopping containers..." -ForegroundColor Gray
docker-compose down --remove-orphans

# Remove dangling images
Write-Host "   Removing dangling images..." -ForegroundColor Gray
docker image prune -f

# Remove unused volumes
Write-Host "   Removing unused volumes..." -ForegroundColor Gray
docker volume prune -f

# Remove unused networks
Write-Host "   Removing unused networks..." -ForegroundColor Gray
docker network prune -f

# Step 2: Clean build cache
Write-Host "Step 2: Cleaning build cache..." -ForegroundColor Yellow
docker builder prune -f

# Step 3: Rebuild with no cache
Write-Host "Step 3: Rebuilding containers with no cache..." -ForegroundColor Yellow

# Copy the API-specific .dockerignore to the parent directory
Write-Host "   Setting up API build context..." -ForegroundColor Gray
Copy-Item ".dockerignore.api" "../.dockerignore" -Force

# Rebuild the API container specifically
Write-Host "   Rebuilding chap2-api container..." -ForegroundColor Gray
docker-compose build --no-cache chap2-api

if ($LASTEXITCODE -eq 0) {
    Write-Host "   PASS: API container built successfully" -ForegroundColor Green
} else {
    Write-Host "   FAIL: API container build failed" -ForegroundColor Red
    Write-Host "   Trying alternative build approach..." -ForegroundColor Yellow
    
    # Try building with a simpler approach
    docker-compose build --no-cache --pull chap2-api
}

# Step 4: Start services
Write-Host "Step 4: Starting services..." -ForegroundColor Yellow
docker-compose up -d

# Step 5: Verify services
Write-Host "Step 5: Verifying services..." -ForegroundColor Yellow
Start-Sleep 10

$services = @("qdrant", "ollama", "langchain-service", "chap2-api", "chap2-webportal")
foreach ($service in $services) {
    $status = docker-compose ps $service --format "table {{.Status}}"
    if ($status -like "*Up*") {
        Write-Host "   PASS: $service is running" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: $service is not running" -ForegroundColor Red
    }
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "Docker build fix completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "If you still have issues, try:" -ForegroundColor Yellow
Write-Host "1. Restart Docker Desktop" -ForegroundColor Gray
Write-Host "2. Run: docker system prune -a" -ForegroundColor Gray
Write-Host "3. Run: docker-compose down && docker-compose up --build" -ForegroundColor Gray 