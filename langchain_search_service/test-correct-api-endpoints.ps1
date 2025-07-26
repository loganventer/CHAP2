Write-Host "========================================" -ForegroundColor Green
Write-Host "Testing Correct CHAP2 API Endpoints" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Test API endpoints with correct /api prefix
Write-Host "Testing API endpoints with /api prefix..." -ForegroundColor Yellow

Write-Host "   Testing /api/health/ping..." -ForegroundColor Gray
try {
    $healthPingResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/health/ping" -TimeoutSec 10
    Write-Host "   PASS: /api/health/ping accessible (Status: $($healthPingResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($healthPingResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/health/ping not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /api/health..." -ForegroundColor Gray
try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/health" -TimeoutSec 10
    Write-Host "   PASS: /api/health accessible (Status: $($healthResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($healthResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/health not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /api/choruses..." -ForegroundColor Gray
try {
    $chorusesResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses accessible (Status: $($chorusesResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($chorusesResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /api/choruses/search..." -ForegroundColor Gray
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses/search?q=test" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses/search accessible (Status: $($searchResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($searchResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses/search not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing /api/choruses/by-name/test..." -ForegroundColor Gray
try {
    $byNameResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses/by-name/test" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses/by-name accessible (Status: $($byNameResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($byNameResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses/by-name not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test a few sample chorus IDs
Write-Host "   Testing /api/choruses with sample ID..." -ForegroundColor Gray
try {
    $sampleIdResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses/000c4908-7650-4172-9234-9c8a2fb4387d" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses/{id} accessible (Status: $($sampleIdResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($sampleIdResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses/{id} not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "API endpoint testing complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Correct API endpoints:" -ForegroundColor Yellow
Write-Host "- /api/health/ping - Health check endpoint" -ForegroundColor White
Write-Host "- /api/choruses - Get all choruses" -ForegroundColor White
Write-Host "- /api/choruses/search?q=query - Search choruses" -ForegroundColor White
Write-Host "- /api/choruses/{id} - Get specific chorus" -ForegroundColor White
Write-Host "- /api/choruses/by-name/{name} - Get chorus by name" -ForegroundColor White

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- CHAP2 API: http://localhost:5001/api" -ForegroundColor White
Write-Host "- Web Portal: http://localhost:5000" -ForegroundColor White
Write-Host "- LangChain Service: http://localhost:8000" -ForegroundColor White 