Write-Host "========================================" -ForegroundColor Green
Write-Host "Testing Service Connectivity" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Test external access (from host)
Write-Host "1. Testing external access (from host):" -ForegroundColor Yellow

Write-Host "   Testing Qdrant (localhost:6333)..." -ForegroundColor Gray
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 5
    Write-Host "   PASS: Qdrant accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Qdrant not accessible from host: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Ollama (localhost:11434)..." -ForegroundColor Gray
try {
    $ollamaResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 5
    Write-Host "   PASS: Ollama accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Ollama not accessible from host: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service (localhost:8000)..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 5
    Write-Host "   PASS: LangChain Service accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible from host: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing CHAP2 API (localhost:5001)..." -ForegroundColor Gray
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 5
    Write-Host "   PASS: CHAP2 API accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: CHAP2 API not accessible from host: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal (localhost:5000)..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 5
    Write-Host "   PASS: Web Portal accessible from host" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible from host: $($_.Exception.Message)" -ForegroundColor Red
}

# Test internal container connectivity
Write-Host ""
Write-Host "2. Testing internal container connectivity:" -ForegroundColor Yellow

Write-Host "   Testing LangChain Service -> Qdrant..." -ForegroundColor Gray
try {
    $internalQdrant = docker exec langchain_search_service-langchain-service-1 curl -s http://qdrant:6333/collections 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Qdrant (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test internal connectivity: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service -> Ollama..." -ForegroundColor Gray
try {
    $internalOllama = docker exec langchain_search_service-langchain-service-1 curl -s http://ollama:11434/api/tags 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can reach Ollama" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot reach Ollama (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test internal connectivity: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal -> LangChain Service..." -ForegroundColor Gray
try {
    $internalLangchain = docker exec langchain_search_service-chap2-webportal-1 curl -s http://langchain-service:8000/health 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Web Portal can reach LangChain Service" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal cannot reach LangChain Service (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test internal connectivity: $($_.Exception.Message)" -ForegroundColor Red
}

# Test search functionality
Write-Host ""
Write-Host "3. Testing search functionality:" -ForegroundColor Yellow

Write-Host "   Testing LangChain search endpoint..." -ForegroundColor Gray
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent_stream?query=test" -TimeoutSec 10
    if ($searchResponse.StatusCode -eq 200) {
        Write-Host "   PASS: LangChain search endpoint working" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain search endpoint returned: $($searchResponse.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: LangChain search endpoint not working: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal search..." -ForegroundColor Gray
try {
    $webSearchResponse = Invoke-WebRequest -Uri "http://localhost:5000/api/search/ai-search?query=test" -TimeoutSec 10
    if ($webSearchResponse.StatusCode -eq 200) {
        Write-Host "   PASS: Web Portal search working" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal search returned: $($webSearchResponse.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Web Portal search not working: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Connectivity test complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green 