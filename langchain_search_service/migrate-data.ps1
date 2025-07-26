Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Data Migration Script" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Check if containers are running
Write-Host "Step 1: Checking if containers are running..." -ForegroundColor Yellow
$containers = docker ps --filter "name=langchain_search_service" --format "table {{.Names}}\t{{.Status}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Check if LangChain service is accessible
Write-Host "Step 2: Checking LangChain service accessibility..." -ForegroundColor Yellow
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Please ensure containers are running with: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Check if Qdrant is accessible
Write-Host "Step 3: Checking Qdrant accessibility..." -ForegroundColor Yellow
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    Write-Host "   PASS: Qdrant accessible (Status: $($qdrantResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Qdrant not accessible: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Please ensure containers are running with: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Check if Ollama models are available
Write-Host "Step 4: Checking Ollama models..." -ForegroundColor Yellow
try {
    $ollamaResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 10
    $models = $ollamaResponse.Content | ConvertFrom-Json
    $modelNames = $models.models | ForEach-Object { $_.name }
    Write-Host "   Available models: $($modelNames -join ', ')" -ForegroundColor Green
    
    if ($modelNames -contains "nomic-embed-text" -and $modelNames -contains "mistral") {
        Write-Host "   PASS: Required models are available" -ForegroundColor Green
    } else {
        Write-Host "   WARNING: Some required models may be missing" -ForegroundColor Yellow
        Write-Host "   Pulling required models..." -ForegroundColor Gray
        
        try {
            docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
            docker exec langchain_search_service-ollama-1 ollama pull mistral
            Write-Host "   PASS: Models pulled successfully" -ForegroundColor Green
        } catch {
            Write-Host "   FAIL: Could not pull models: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "   FAIL: Ollama not accessible: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Run data migration
Write-Host "Step 5: Running data migration..." -ForegroundColor Yellow
try {
    Write-Host "   Executing migration script..." -ForegroundColor Gray
    $migrationOutput = docker exec langchain_search_service-langchain-service-1 python migrate_data.py 2>&1
    Write-Host "   Migration output:" -ForegroundColor Gray
    Write-Host $migrationOutput -ForegroundColor Gray
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Data migration completed successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Data migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Could not run data migration: $($_.Exception.Message)" -ForegroundColor Red
}

# Verify migration results
Write-Host "Step 6: Verifying migration results..." -ForegroundColor Yellow
try {
    # Check Qdrant collections
    $collectionsResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    $collections = $collectionsResponse.Content | ConvertFrom-Json
    
    $chorusCollection = $collections.collections | Where-Object { $_.name -eq "chorus-vectors" }
    if ($chorusCollection) {
        Write-Host "   PASS: chorus-vectors collection exists" -ForegroundColor Green
        
        # Get collection info
        $collectionInfoResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections/chorus-vectors" -TimeoutSec 10
        $collectionInfo = $collectionInfoResponse.Content | ConvertFrom-Json
        
        $vectorCount = $collectionInfo.result.vectors_count
        Write-Host "   Vector count: $vectorCount" -ForegroundColor Green
        
        if ($vectorCount -gt 0) {
            Write-Host "   PASS: Data successfully migrated to vector store" -ForegroundColor Green
        } else {
            Write-Host "   WARNING: Vector store appears to be empty" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   FAIL: chorus-vectors collection not found" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Could not verify migration results: $($_.Exception.Message)" -ForegroundColor Red
}

# Test search functionality
Write-Host "Step 7: Testing search functionality..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent" -Method POST -ContentType "application/json" -Body '{"query":"test","k":1}' -TimeoutSec 10
    Write-Host "   PASS: Search functionality working (Status: $($searchResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Search functionality not working: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Data migration verification complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "- Open http://localhost:5000 to test the web portal" -ForegroundColor White
Write-Host "- Try searching for choruses to verify functionality" -ForegroundColor White
Write-Host "- Check logs with: docker-compose logs -f langchain-service" -ForegroundColor White 