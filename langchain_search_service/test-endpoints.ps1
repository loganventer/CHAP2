Write-Host "========================================" -ForegroundColor Green
Write-Host "Testing All Service Endpoints" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Test CHAP2 API endpoints
Write-Host "Testing CHAP2 API endpoints..." -ForegroundColor Yellow

Write-Host "   Testing CHAP2 API /health/ping..." -ForegroundColor Gray
try {
    $apiPingResponse = Invoke-WebRequest -Uri "http://localhost:5001/health/ping" -TimeoutSec 10
    Write-Host "   PASS: CHAP2 API /health/ping accessible (Status: $($apiPingResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($apiPingResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: CHAP2 API /health/ping not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing CHAP2 API /choruses..." -ForegroundColor Gray
try {
    $apiChorusesResponse = Invoke-WebRequest -Uri "http://localhost:5001/choruses" -TimeoutSec 10
    Write-Host "   PASS: CHAP2 API /choruses accessible (Status: $($apiChorusesResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: CHAP2 API /choruses not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Web Portal endpoints
Write-Host "Testing Web Portal endpoints..." -ForegroundColor Yellow

Write-Host "   Testing Web Portal /..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 10
    Write-Host "   PASS: Web Portal accessible (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal /api/search/ai-search..." -ForegroundColor Gray
try {
    $webSearchResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/search/ai-search" -Method POST -ContentType "application/json" -Body '{"query":"test"}' -TimeoutSec 10
    Write-Host "   PASS: Web Portal search endpoint accessible (Status: $($webSearchResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal search endpoint not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test LangChain Service endpoints
Write-Host "Testing LangChain Service endpoints..." -ForegroundColor Yellow

Write-Host "   Testing LangChain Service /health..." -ForegroundColor Gray
try {
    $langchainHealthResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service /health accessible (Status: $($langchainHealthResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service /health not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service /search_intelligent..." -ForegroundColor Gray
try {
    $langchainSearchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent" -Method POST -ContentType "application/json" -Body '{"query":"test"}' -TimeoutSec 10
    Write-Host "   PASS: LangChain Service search endpoint accessible (Status: $($langchainSearchResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service search endpoint not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Qdrant endpoints
Write-Host "Testing Qdrant endpoints..." -ForegroundColor Yellow

Write-Host "   Testing Qdrant /collections..." -ForegroundColor Gray
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    Write-Host "   PASS: Qdrant accessible (Status: $($qdrantResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Qdrant not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Ollama endpoints
Write-Host "Testing Ollama endpoints..." -ForegroundColor Yellow

Write-Host "   Testing Ollama /api/tags..." -ForegroundColor Gray
try {
    $ollamaResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 10
    Write-Host "   PASS: Ollama accessible (Status: $($ollamaResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Ollama not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Endpoint testing complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- CHAP2 API: http://localhost:5001" -ForegroundColor White
Write-Host "- Web Portal: http://localhost:5000" -ForegroundColor White
Write-Host "- LangChain Service: http://localhost:8000" -ForegroundColor White
Write-Host "- Qdrant: http://localhost:6333" -ForegroundColor White
Write-Host "- Ollama: http://localhost:11434" -ForegroundColor White 