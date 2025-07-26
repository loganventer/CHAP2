#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test all endpoints to identify the exact issue on Windows
.DESCRIPTION
    Tests different ports and endpoints to find where the web portal is accessible
#>

Write-Host "=== Windows Endpoint Testing ===" -ForegroundColor Cyan
Write-Host ""

# Test different ports for the web portal
$ports = @(5000, 5001, 5002, 8080, 3000)
$endpoints = @("/Home/Search?q=test", "/Home/IntelligentSearch", "/Home/RagSearch")

foreach ($port in $ports) {
    Write-Host "Testing port $port..." -ForegroundColor Yellow
    
    foreach ($endpoint in $endpoints) {
        $url = "http://localhost:$port$endpoint"
        Write-Host "  Testing: $url" -ForegroundColor White
        
        try {
            if ($endpoint -eq "/Home/IntelligentSearch" -or $endpoint -eq "/Home/RagSearch") {
                # POST request for AI endpoints
                $body = @{
                    query = "test"
                    maxResults = 3
                } | ConvertTo-Json
                
                $response = Invoke-RestMethod -Uri $url -Method Post -Body $body -ContentType "application/json" -TimeoutSec 5
                Write-Host "    ✅ SUCCESS: $endpoint on port $port" -ForegroundColor Green
            } else {
                # GET request for search endpoint
                $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 5
                Write-Host "    ✅ SUCCESS: $endpoint on port $port" -ForegroundColor Green
            }
        } catch {
            Write-Host "    ❌ FAILED: $endpoint on port $port - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    Write-Host ""
}

# Test container internal connectivity
Write-Host "Testing container internal connectivity..." -ForegroundColor Yellow
try {
    $webPortalContainer = docker-compose ps -q chap2-webportal
    if ($webPortalContainer) {
        Write-Host "  Testing web portal container internal access..." -ForegroundColor White
        $internalTest = docker exec $webPortalContainer curl -s -m 5 "http://localhost:5000/Home/Search?q=test"
        if ($internalTest) {
            Write-Host "    ✅ SUCCESS: Web portal responding internally" -ForegroundColor Green
        } else {
            Write-Host "    ❌ FAILED: Web portal not responding internally" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "    ❌ ERROR: Could not test container connectivity" -ForegroundColor Red
}

# Check actual port mapping
Write-Host "Checking actual port mapping..." -ForegroundColor Yellow
try {
    $portMapping = docker port langchain_search_service-chap2-webportal-1
    Write-Host "  Actual port mapping:" -ForegroundColor White
    Write-Host $portMapping -ForegroundColor Green
} catch {
    Write-Host "    ❌ ERROR: Could not get port mapping" -ForegroundColor Red
}

# Test if it's a routing issue
Write-Host "Testing routing issues..." -ForegroundColor Yellow
$testUrls = @(
    "http://localhost:5002/",
    "http://localhost:5002/Home",
    "http://localhost:5002/Home/Index",
    "http://localhost:5000/",
    "http://localhost:5000/Home"
)

foreach ($url in $testUrls) {
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 5
        Write-Host "  ✅ SUCCESS: $url (Status: $($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ FAILED: $url - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "If any endpoint works, the issue is likely:" -ForegroundColor Yellow
Write-Host "1. Wrong port in JavaScript (should match working port)" -ForegroundColor White
Write-Host "2. Windows Firewall blocking specific endpoints" -ForegroundColor White
Write-Host "3. Docker port mapping issue" -ForegroundColor White
Write-Host ""
Write-Host "If no endpoints work, the issue is likely:" -ForegroundColor Yellow
Write-Host "1. Web portal container not running" -ForegroundColor White
Write-Host "2. Port conflict with other services" -ForegroundColor White
Write-Host "3. Docker Desktop networking issue" -ForegroundColor White 