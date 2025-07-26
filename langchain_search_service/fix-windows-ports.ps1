#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fix Windows Docker port and networking issues
.DESCRIPTION
    Diagnoses and fixes common Windows Docker networking problems
#>

Write-Host "=== Windows Docker Port Diagnosis ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check what's actually running
Write-Host "Step 1: Checking running containers and ports..." -ForegroundColor Yellow
try {
    $containers = docker-compose ps --format json | ConvertFrom-Json
    Write-Host "   Container status:" -ForegroundColor Green
    foreach ($container in $containers) {
        Write-Host "     - $($container.Service): $($container.State) on ports $($container.Ports)" -ForegroundColor Green
    }
} catch {
    Write-Host "   ERROR: Could not get container status" -ForegroundColor Red
}

# Step 2: Check what's listening on port 5002
Write-Host "Step 2: Checking what's listening on port 5002..." -ForegroundColor Yellow
try {
    $port5002 = netstat -an | findstr :5002
    if ($port5002) {
        Write-Host "   Port 5002 is in use:" -ForegroundColor Green
        Write-Host $port5002 -ForegroundColor White
    } else {
        Write-Host "   WARNING: Nothing is listening on port 5002" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ERROR: Could not check port 5002" -ForegroundColor Red
}

# Step 3: Check Docker Compose port mapping
Write-Host "Step 3: Checking Docker Compose port mapping..." -ForegroundColor Yellow
try {
    $composeConfig = docker-compose config
    $portMapping = $composeConfig | Select-String "5002"
    if ($portMapping) {
        Write-Host "   Port mapping found:" -ForegroundColor Green
        Write-Host $portMapping -ForegroundColor White
    } else {
        Write-Host "   WARNING: No port 5002 mapping found in docker-compose" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ERROR: Could not check compose config" -ForegroundColor Red
}

# Step 4: Test container connectivity
Write-Host "Step 4: Testing container connectivity..." -ForegroundColor Yellow
try {
    $webPortalContainer = docker-compose ps -q chap2-webportal
    if ($webPortalContainer) {
        Write-Host "   Testing web portal container internal connectivity..." -ForegroundColor Green
        $internalTest = docker exec $webPortalContainer curl -s -m 5 "http://localhost:5000/Home/Search?q=test"
        if ($internalTest) {
            Write-Host "   PASS: Web portal is responding internally" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: Web portal not responding internally" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "   ERROR: Could not test container connectivity" -ForegroundColor Red
}

# Step 5: Check if it's a port conflict
Write-Host "Step 5: Checking for port conflicts..." -ForegroundColor Yellow
try {
    $allPorts = netstat -an | findstr :500
    Write-Host "   All processes using port 500x:" -ForegroundColor Green
    Write-Host $allPorts -ForegroundColor White
} catch {
    Write-Host "   ERROR: Could not check port conflicts" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Suggested Fixes ===" -ForegroundColor Cyan
Write-Host ""

# Fix 1: Restart containers
Write-Host "Fix 1: Restarting containers..." -ForegroundColor Yellow
try {
    docker-compose restart
    Write-Host "   Containers restarted" -ForegroundColor Green
} catch {
    Write-Host "   ERROR: Could not restart containers" -ForegroundColor Red
}

# Fix 2: Check if port 5002 is actually mapped
Write-Host "Fix 2: Checking actual port mapping..." -ForegroundColor Yellow
try {
    $actualPorts = docker port langchain_search_service-chap2-webportal-1
    Write-Host "   Actual web portal port mapping:" -ForegroundColor Green
    Write-Host $actualPorts -ForegroundColor White
} catch {
    Write-Host "   ERROR: Could not get actual port mapping" -ForegroundColor Red
}

# Fix 3: Try alternative port
Write-Host "Fix 3: Testing alternative port..." -ForegroundColor Yellow
try {
    $altResponse = Invoke-RestMethod -Uri "http://localhost:5000/Home/Search?q=test" -Method Get -TimeoutSec 5
    Write-Host "   SUCCESS: Web portal accessible on port 5000" -ForegroundColor Green
    Write-Host "   Try accessing: http://localhost:5000" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web portal not accessible on port 5000 either" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Try accessing: http://localhost:5000 (instead of 5002)" -ForegroundColor White
Write-Host "2. Check if Windows Firewall is blocking the connection" -ForegroundColor White
Write-Host "3. Try: docker-compose down && docker-compose up -d" -ForegroundColor White
Write-Host "4. Check Docker Desktop settings for port forwarding" -ForegroundColor White 