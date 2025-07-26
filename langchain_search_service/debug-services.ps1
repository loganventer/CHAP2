Write-Host "========================================" -ForegroundColor Green
Write-Host "Detailed Service Debugging" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check container names and status
Write-Host "1. Container Status:" -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Check container environment variables
Write-Host ""
Write-Host "2. Container Environment Variables:" -ForegroundColor Yellow

Write-Host "   LangChain Service Environment:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-langchain-service-1 env | Select-String "QDRANT_URL|OLLAMA_URL" 2>$null
} catch {
    Write-Host "   Cannot access LangChain container" -ForegroundColor Red
}

Write-Host "   Web Portal Environment:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-chap2-webportal-1 env | Select-String "LangChainService|ApiService|Qdrant|Ollama" 2>$null
} catch {
    Write-Host "   Cannot access Web Portal container" -ForegroundColor Red
}

# Check service configurations
Write-Host ""
Write-Host "3. Service Configurations:" -ForegroundColor Yellow

Write-Host "   LangChain Service Configuration:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-langchain-service-1 cat /app/main.py | Select-String "QDRANT_URL|OLLAMA_URL" 2>$null
} catch {
    Write-Host "   Cannot read LangChain configuration" -ForegroundColor Red
}

# Check if services are listening on correct ports
Write-Host ""
Write-Host "4. Service Port Bindings:" -ForegroundColor Yellow

Write-Host "   LangChain Service Ports:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-langchain-service-1 netstat -tlnp 2>$null
} catch {
    Write-Host "   Cannot check LangChain ports" -ForegroundColor Red
}

Write-Host "   Web Portal Ports:" -ForegroundColor Gray
try {
    docker exec langchain_search_service-chap2-webportal-1 netstat -tlnp 2>$null
} catch {
    Write-Host "   Cannot check Web Portal ports" -ForegroundColor Red
}

# Test direct service calls
Write-Host ""
Write-Host "5. Direct Service Tests:" -ForegroundColor Yellow

Write-Host "   Testing LangChain service directly:" -ForegroundColor Gray
try {
    $langchainTest = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent_stream?query=test" -TimeoutSec 10
    Write-Host "   LangChain Response Status: $($langchainTest.StatusCode)" -ForegroundColor Gray
    Write-Host "   LangChain Response Headers: $($langchainTest.Headers)" -ForegroundColor Gray
} catch {
    Write-Host "   LangChain service error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal search endpoint:" -ForegroundColor Gray
try {
    $webTest = Invoke-WebRequest -Uri "http://localhost:5000/api/search/ai-search?query=test" -TimeoutSec 10
    Write-Host "   Web Portal Response Status: $($webTest.StatusCode)" -ForegroundColor Gray
    Write-Host "   Web Portal Response Headers: $($webTest.Headers)" -ForegroundColor Gray
} catch {
    Write-Host "   Web Portal service error: $($_.Exception.Message)" -ForegroundColor Red
}

# Check container logs for errors
Write-Host ""
Write-Host "6. Container Logs (Last 20 lines):" -ForegroundColor Yellow

Write-Host "   LangChain Service Logs:" -ForegroundColor Gray
try {
    docker logs --tail 20 langchain_search_service-langchain-service-1 2>$null
} catch {
    Write-Host "   Cannot access LangChain logs" -ForegroundColor Red
}

Write-Host ""
Write-Host "   Web Portal Logs:" -ForegroundColor Gray
try {
    docker logs --tail 20 langchain_search_service-chap2-webportal-1 2>$null
} catch {
    Write-Host "   Cannot access Web Portal logs" -ForegroundColor Red
}

# Check network connectivity
Write-Host ""
Write-Host "7. Network Connectivity:" -ForegroundColor Yellow

Write-Host "   Testing LangChain -> Qdrant:" -ForegroundColor Gray
try {
    $qdrantTest = docker exec langchain_search_service-langchain-service-1 curl -v http://qdrant:6333/collections 2>&1
    Write-Host "   Qdrant connectivity: $qdrantTest" -ForegroundColor Gray
} catch {
    Write-Host "   Qdrant connectivity failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain -> Ollama:" -ForegroundColor Gray
try {
    $ollamaTest = docker exec langchain_search_service-langchain-service-1 curl -v http://ollama:11434/api/tags 2>&1
    Write-Host "   Ollama connectivity: $ollamaTest" -ForegroundColor Gray
} catch {
    Write-Host "   Ollama connectivity failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Debugging complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green 