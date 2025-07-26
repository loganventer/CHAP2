Write-Host "========================================" -ForegroundColor Green
Write-Host "Testing CHAP2 API Endpoints" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Test various API endpoints
Write-Host "Testing API endpoints..." -ForegroundColor Yellow

Write-Host "   Testing /health/ping..." -ForegroundColor Gray
try {
    $healthPingResponse = Invoke-WebRequest -Uri "http://localhost:5001/health/ping" -TimeoutSec 10
    Write-Host "   PASS: /health/ping accessible (Status: $($healthPingResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($healthPingResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /health/ping not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /health..." -ForegroundColor Gray
try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 10
    Write-Host "   PASS: /health accessible (Status: $($healthResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($healthResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /health not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /choruses..." -ForegroundColor Gray
try {
    $chorusesResponse = Invoke-WebRequest -Uri "http://localhost:5001/choruses" -TimeoutSec 10
    Write-Host "   PASS: /choruses accessible (Status: $($chorusesResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($chorusesResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /choruses not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /choruses/search..." -ForegroundColor Gray
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:5001/choruses/search?q=test" -TimeoutSec 10
    Write-Host "   PASS: /choruses/search accessible (Status: $($searchResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($searchResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /choruses/search not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing root /..." -ForegroundColor Gray
try {
    $rootResponse = Invoke-WebRequest -Uri "http://localhost:5001/" -TimeoutSec 10
    Write-Host "   PASS: Root / accessible (Status: $($rootResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($rootResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: Root / not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /api/choruses..." -ForegroundColor Gray
try {
    $apiChorusesResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses accessible (Status: $($apiChorusesResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($apiChorusesResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test with different HTTP methods
Write-Host "   Testing OPTIONS request..." -ForegroundColor Gray
try {
    $optionsResponse = Invoke-WebRequest -Uri "http://localhost:5001/choruses" -Method OPTIONS -TimeoutSec 10
    Write-Host "   PASS: OPTIONS request successful (Status: $($optionsResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: OPTIONS request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "API endpoint testing complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Available endpoints (if working):" -ForegroundColor Yellow
Write-Host "- /health/ping - Health check endpoint" -ForegroundColor White
Write-Host "- /choruses - Get all choruses" -ForegroundColor White
Write-Host "- /choruses/search?q=query - Search choruses" -ForegroundColor White
Write-Host "- /choruses/{id} - Get specific chorus" -ForegroundColor White
Write-Host "- /choruses/by-name/{name} - Get chorus by name" -ForegroundColor White 