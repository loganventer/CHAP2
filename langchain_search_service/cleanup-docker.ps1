Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Docker Cleanup Script" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Show current disk usage
Write-Host "Step 1: Current Docker disk usage..." -ForegroundColor Yellow
try {
    $diskUsage = docker system df
    Write-Host $diskUsage -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: Cannot get disk usage" -ForegroundColor Red
}

# Step 2: Stop and remove containers
Write-Host "Step 2: Stopping and removing containers..." -ForegroundColor Yellow
try {
    docker-compose down
    Write-Host "   PASS: Containers stopped and removed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not stop containers" -ForegroundColor Red
}

# Step 3: Remove dangling images
Write-Host "Step 3: Removing dangling images..." -ForegroundColor Yellow
try {
    $danglingImages = docker images -f "dangling=true" -q
    if ($danglingImages) {
        Write-Host "   Found dangling images, removing..." -ForegroundColor Gray
        docker rmi $danglingImages
        Write-Host "   PASS: Dangling images removed" -ForegroundColor Green
    } else {
        Write-Host "   No dangling images found" -ForegroundColor Green
    }
} catch {
    Write-Host "   FAIL: Could not remove dangling images" -ForegroundColor Red
}

# Step 4: Remove unused images
Write-Host "Step 4: Removing unused images..." -ForegroundColor Yellow
try {
    docker image prune -f
    Write-Host "   PASS: Unused images removed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not remove unused images" -ForegroundColor Red
}

# Step 5: Remove unused containers
Write-Host "Step 5: Removing unused containers..." -ForegroundColor Yellow
try {
    docker container prune -f
    Write-Host "   PASS: Unused containers removed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not remove unused containers" -ForegroundColor Red
}

# Step 6: Remove unused networks
Write-Host "Step 6: Removing unused networks..." -ForegroundColor Yellow
try {
    docker network prune -f
    Write-Host "   PASS: Unused networks removed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not remove unused networks" -ForegroundColor Red
}

# Step 7: Remove unused volumes
Write-Host "Step 7: Removing unused volumes..." -ForegroundColor Yellow
try {
    docker volume prune -f
    Write-Host "   PASS: Unused volumes removed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not remove unused volumes" -ForegroundColor Red
}

# Step 8: Show final disk usage
Write-Host "Step 8: Final Docker disk usage..." -ForegroundColor Yellow
try {
    $finalDiskUsage = docker system df
    Write-Host $finalDiskUsage -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: Cannot get final disk usage" -ForegroundColor Red
}

# Step 9: Show remaining images
Write-Host "Step 9: Remaining images..." -ForegroundColor Yellow
try {
    $remainingImages = docker images
    Write-Host $remainingImages -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: Cannot list remaining images" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Cleanup completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "To rebuild your services:" -ForegroundColor Yellow
Write-Host "1. Run: docker-compose up -d" -ForegroundColor White
Write-Host "2. Or run: .\start-windows-gpu-detection.ps1" -ForegroundColor White 