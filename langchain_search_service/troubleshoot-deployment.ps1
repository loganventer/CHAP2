param(
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Deployment Troubleshooting" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check if Docker is running
Write-Host "1. Checking Docker status..." -ForegroundColor Yellow
$dockerInfo = docker info 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "   Docker is running" -ForegroundColor Green
} else {
    Write-Host "   Docker is not running or not accessible" -ForegroundColor Red
    exit 1
}

# Check running containers
Write-Host "2. Checking running containers..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host "   Running containers:" -ForegroundColor Gray
Write-Host $containers -ForegroundColor Gray

# Check if all expected containers are running
$expectedContainers = @("qdrant", "ollama", "langchain-service", "chap2-api", "chap2-webportal")
$missingContainers = @()

foreach ($container in $expectedContainers) {
    if ($containers -notmatch $container) {
        $missingContainers += $container
    }
}

if ($missingContainers.Count -gt 0) {
    Write-Host "   Missing containers: $($missingContainers -join ', ')" -ForegroundColor Red
} else {
    Write-Host "   All expected containers are running" -ForegroundColor Green
}

# Check service health
Write-Host "3. Checking service health..." -ForegroundColor Yellow

# Check Qdrant
Write-Host "   Testing Qdrant..." -ForegroundColor Gray
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/health" -TimeoutSec 5 2>$null
    if ($qdrantResponse.StatusCode -eq 200) {
        Write-Host "   Qdrant is healthy" -ForegroundColor Green
    } else {
        Write-Host "   Qdrant returned status: $($qdrantResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Qdrant is not responding" -ForegroundColor Red
}

# Check Ollama
Write-Host "   Testing Ollama..." -ForegroundColor Gray
try {
    $ollamaResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 5 2>$null
    if ($ollamaResponse.StatusCode -eq 200) {
        Write-Host "   Ollama is responding" -ForegroundColor Green
        $models = $ollamaResponse.Content | ConvertFrom-Json
        Write-Host "   Available models: $($models.models.Count)" -ForegroundColor Gray
    } else {
        Write-Host "   Ollama returned status: $($ollamaResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Ollama is not responding" -ForegroundColor Red
}

# Check LangChain Service
Write-Host "   Testing LangChain Service..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 5 2>$null
    if ($langchainResponse.StatusCode -eq 200) {
        Write-Host "   LangChain Service is healthy" -ForegroundColor Green
    } else {
        Write-Host "   LangChain Service returned status: $($langchainResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   LangChain Service is not responding" -ForegroundColor Red
}

# Check CHAP2 API
Write-Host "   Testing CHAP2 API..." -ForegroundColor Gray
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5001/health" -TimeoutSec 5 2>$null
    if ($apiResponse.StatusCode -eq 200) {
        Write-Host "   CHAP2 API is healthy" -ForegroundColor Green
    } else {
        Write-Host "   CHAP2 API returned status: $($apiResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   CHAP2 API is not responding" -ForegroundColor Red
}

# Check Web Portal
Write-Host "   Testing Web Portal..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 5 2>$null
    if ($webResponse.StatusCode -eq 200) {
        Write-Host "   Web Portal is responding" -ForegroundColor Green
    } else {
        Write-Host "   Web Portal returned status: $($webResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   Web Portal is not responding" -ForegroundColor Red
}

# Check vector store data
Write-Host "4. Checking vector store data..." -ForegroundColor Yellow
try {
    $vectorResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections/chorus-vectors" -TimeoutSec 5 2>$null
    if ($vectorResponse.StatusCode -eq 200) {
        $collectionInfo = $vectorResponse.Content | ConvertFrom-Json
        $vectorCount = $collectionInfo.result.points_count
        Write-Host "   Vector store has $vectorCount vectors" -ForegroundColor Green
        
        if ($vectorCount -eq 0) {
            Write-Host "   WARNING: Vector store is empty!" -ForegroundColor Red
            Write-Host "   This is why the list is not populating." -ForegroundColor Red
        }
    } else {
        Write-Host "   Vector store collection not found" -ForegroundColor Red
    }
} catch {
    Write-Host "   Cannot access vector store" -ForegroundColor Red
}

# Test search functionality
Write-Host "5. Testing search functionality..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent_stream?query=test" -TimeoutSec 10 2>$null
    if ($searchResponse.StatusCode -eq 200) {
        Write-Host "   LangChain search is working" -ForegroundColor Green
    } else {
        Write-Host "   LangChain search returned status: $($searchResponse.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   LangChain search is not working" -ForegroundColor Red
}

# Check container logs for errors
if ($Verbose) {
    Write-Host "6. Checking container logs..." -ForegroundColor Yellow
    
    $containers = docker ps --format "{{.Names}}" 2>$null
    foreach ($container in $containers) {
        Write-Host "   Logs for $container:" -ForegroundColor Gray
        docker logs --tail 10 $container 2>$null
        Write-Host ""
    }
}

Write-Host "========================================" -ForegroundColor Green
Write-Host "Troubleshooting complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Provide recommendations
Write-Host ""
Write-Host "Recommendations:" -ForegroundColor Yellow
Write-Host "1. If vector store is empty, run data migration:" -ForegroundColor White
Write-Host "   docker exec langchain_search_service-langchain-service-1 python migrate_data.py" -ForegroundColor Gray
Write-Host ""
Write-Host "2. If services are not responding, restart containers:" -ForegroundColor White
Write-Host "   docker-compose restart" -ForegroundColor Gray
Write-Host ""
Write-Host "3. For detailed logs, run with -Verbose flag:" -ForegroundColor White
Write-Host "   .\troubleshoot-deployment.ps1 -Verbose" -ForegroundColor Gray 