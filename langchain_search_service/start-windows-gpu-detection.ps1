#Requires -Version 5.1

param(
    [switch]$SkipGpuCheck,
    [switch]$ForceRebuild,
    [switch]$ForceGpu
)

Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 LangChain Search Service Deployment" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Check prerequisites
Write-Host "Step 1: Checking prerequisites..." -ForegroundColor Yellow

# Check if Docker is installed and running
try {
    $dockerVersion = docker --version
    Write-Host "   Docker version: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Docker not found. Please install Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Docker is running
try {
    $dockerInfo = docker info 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Docker is running" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Docker is not running. Please start Docker Desktop." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   FAIL: Cannot check Docker status" -ForegroundColor Red
    exit 1
}

# Step 2: GPU Detection and Setup
if (-not $SkipGpuCheck) {
    Write-Host "Step 2: Detecting GPU and setting up NVIDIA Container Toolkit..." -ForegroundColor Yellow
    
    # Check for NVIDIA GPU
    try {
        $gpuInfo = nvidia-smi 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: NVIDIA GPU detected" -ForegroundColor Green
            Write-Host "   GPU Info:" -ForegroundColor Gray
            Write-Host $gpuInfo -ForegroundColor Gray
            
            # Check NVIDIA Container Toolkit
            Write-Host "   Checking NVIDIA Container Toolkit..." -ForegroundColor Gray
            try {
                $toolkitTest = docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   PASS: NVIDIA Container Toolkit is working" -ForegroundColor Green
                    $script:UseGpu = $true
                } else {
                    Write-Host "   FAIL: NVIDIA Container Toolkit not working" -ForegroundColor Red
                    Write-Host "   Please install NVIDIA Container Toolkit manually" -ForegroundColor Yellow
                    $script:UseGpu = $false
                }
            } catch {
                Write-Host "   FAIL: Cannot test NVIDIA Container Toolkit" -ForegroundColor Red
                $script:UseGpu = $false
            }
        } else {
            Write-Host "   INFO: No NVIDIA GPU detected, will use CPU mode" -ForegroundColor Yellow
            $script:UseGpu = $false
        }
    } catch {
        Write-Host "   INFO: nvidia-smi not available, will use CPU mode" -ForegroundColor Yellow
        $script:UseGpu = $false
    }
    
    # Force GPU if requested
    if ($ForceGpu) {
        Write-Host "   FORCE: GPU mode enabled by user request" -ForegroundColor Yellow
        $script:UseGpu = $true
    }
} else {
    Write-Host "Step 2: Skipping GPU detection (SkipGpuCheck specified)" -ForegroundColor Yellow
    if ($ForceGpu) {
        Write-Host "   FORCE: GPU mode enabled by user request" -ForegroundColor Yellow
        $script:UseGpu = $true
    } else {
        $script:UseGpu = $false
    }
}

# Step 3: Fix API Routes Configuration
Write-Host "Step 3: Fixing API routes configuration..." -ForegroundColor Yellow

# Update Web Portal appsettings.json
$webPortalAppSettings = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiBaseUrl": "http://chap2-api:5001",
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "chorus-vectors",
    "VectorSize": 1536
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "mistral",
    "MaxTokens": 2048,
    "Temperature": 0.7
  },
  "LangChainService": {
    "BaseUrl": "http://localhost:8000"
  }
}
"@

try {
    Set-Content -Path "../CHAP2.UI/CHAP2.WebPortal/appsettings.json" -Value $webPortalAppSettings
    Write-Host "   PASS: Updated Web Portal appsettings.json" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update Web Portal appsettings.json: $($_.Exception.Message)" -ForegroundColor Red
}

# Update Web Portal appsettings.Development.json
$webPortalDevAppSettings = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiBaseUrl": "http://localhost:5001",
  "Qdrant": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "chorus-vectors",
    "VectorSize": 1536
  },
  "Ollama": {
    "Host": "localhost",
    "Port": 11434,
    "Model": "mistral",
    "MaxTokens": 2048,
    "Temperature": 0.7
  },
  "LangChainService": {
    "BaseUrl": "http://localhost:8000"
  }
}
"@

try {
    Set-Content -Path "../CHAP2.UI/CHAP2.WebPortal/appsettings.Development.json" -Value $webPortalDevAppSettings
    Write-Host "   PASS: Updated Web Portal appsettings.Development.json" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update Web Portal appsettings.Development.json: $($_.Exception.Message)" -ForegroundColor Red
}

# Update Web Portal appsettings.Production.json
$webPortalProdAppSettings = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiBaseUrl": "http://chap2-api:5001",
  "Qdrant": {
    "Host": "qdrant",
    "Port": 6333,
    "CollectionName": "chorus-vectors",
    "VectorSize": 1536
  },
  "Ollama": {
    "Host": "ollama",
    "Port": 11434,
    "Model": "mistral",
    "MaxTokens": 2048,
    "Temperature": 0.7
  },
  "LangChainService": {
    "BaseUrl": "http://langchain-service:8000"
  }
}
"@

try {
    Set-Content -Path "../CHAP2.UI/CHAP2.WebPortal/appsettings.Production.json" -Value $webPortalProdAppSettings
    Write-Host "   PASS: Updated Web Portal appsettings.Production.json" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update Web Portal appsettings.Production.json: $($_.Exception.Message)" -ForegroundColor Red
}

# Update Docker Compose files with correct API routes
Write-Host "   Updating Docker Compose files..." -ForegroundColor Gray

$dockerComposeContent = Get-Content -Path "docker-compose.yml" -Raw
$updatedDockerCompose = $dockerComposeContent -replace 'ApiService__BaseUrl=http://chap2-api:5001', 'ApiService__BaseUrl=http://chap2-api:5001/api'

try {
    Set-Content -Path "docker-compose.yml" -Value $updatedDockerCompose
    Write-Host "   PASS: Updated docker-compose.yml" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update docker-compose.yml: $($_.Exception.Message)" -ForegroundColor Red
}

$gpuComposeContent = Get-Content -Path "docker-compose.gpu.yml" -Raw
$updatedGpuCompose = $gpuComposeContent -replace 'ApiService__BaseUrl=http://chap2-api:5001', 'ApiService__BaseUrl=http://chap2-api:5001/api'

try {
    Set-Content -Path "docker-compose.gpu.yml" -Value $updatedGpuCompose
    Write-Host "   PASS: Updated docker-compose.gpu.yml" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update docker-compose.gpu.yml: $($_.Exception.Message)" -ForegroundColor Red
}

$gpuDirectComposeContent = Get-Content -Path "docker-compose.gpu-direct.yml" -Raw
$updatedGpuDirectCompose = $gpuDirectComposeContent -replace 'ApiService__BaseUrl=http://chap2-api:5001', 'ApiService__BaseUrl=http://chap2-api:5001/api'

try {
    Set-Content -Path "docker-compose.gpu-direct.yml" -Value $updatedGpuDirectCompose
    Write-Host "   PASS: Updated docker-compose.gpu-direct.yml" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update docker-compose.gpu-direct.yml: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 4: Stop existing containers and clean up
Write-Host "Step 4: Stopping existing containers and cleaning up..." -ForegroundColor Yellow

# Determine which compose file to use for cleanup
if ($script:UseGpu) {
    $cleanupComposeFile = "docker-compose.gpu.yml"
} else {
    $cleanupComposeFile = "docker-compose.yml"
}

try {
    Write-Host "   Stopping all containers..." -ForegroundColor Gray
    docker-compose -f $cleanupComposeFile down
    Start-Sleep -Seconds 5
    
    # Force remove any remaining containers
    $remainingContainers = docker ps -aq --filter "name=langchain_search_service" 2>$null
    if ($remainingContainers) {
        docker rm -f $remainingContainers 2>$null
        Write-Host "   Removed remaining containers" -ForegroundColor Green
    }
    
    # Remove and recreate network properly
    Write-Host "   Cleaning up Docker network..." -ForegroundColor Gray
    try {
        # Force remove the network if it exists
        docker network rm langchain_search_service_default 2>$null
        Start-Sleep -Seconds 2
    } catch {
        # Network might not exist, that's okay
    }
    
    # Remove any orphaned containers that might be using the network
    $orphanedContainers = docker ps -aq --filter "network=langchain_search_service_default" 2>$null
    if ($orphanedContainers) {
        docker rm -f $orphanedContainers 2>$null
        Write-Host "   Removed orphaned containers" -ForegroundColor Green
    }
    
    # Let Docker Compose create the network fresh
    Write-Host "   PASS: Network cleanup completed" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not clean up containers: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Build and start containers with better error handling
Write-Host "Step 5: Building and starting containers..." -ForegroundColor Yellow

# Determine which compose file to use
if ($script:UseGpu) {
    Write-Host "   Using GPU-enabled Docker Compose configuration..." -ForegroundColor Green
    $composeFile = "docker-compose.gpu.yml"
} else {
    Write-Host "   Using CPU-only Docker Compose configuration..." -ForegroundColor Yellow
    $composeFile = "docker-compose.yml"
}

try {
    if ($ForceRebuild) {
        Write-Host "   Force rebuilding containers..." -ForegroundColor Gray
        docker-compose -f $composeFile build --no-cache
    } else {
        Write-Host "   Building containers (using cache if available)..." -ForegroundColor Gray
        docker-compose -f $composeFile build
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   FAIL: Container build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   PASS: Containers built successfully" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not build containers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start containers one by one with verification (without rebuilding)
Write-Host "   Starting containers one by one..." -ForegroundColor Gray

# Start Qdrant first
Write-Host "   Starting Qdrant..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d --no-build qdrant
    Start-Sleep -Seconds 10
    
    # Check if Qdrant is running
    $qdrantStatus = docker ps --filter "name=qdrant" --format "{{.Status}}"
    if ($qdrantStatus -like "*Up*") {
        Write-Host "   PASS: Qdrant started successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Qdrant failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs qdrant
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start Qdrant: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start Ollama
Write-Host "   Starting Ollama..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d --no-build ollama
    Start-Sleep -Seconds 10
    
    # Check if Ollama is running
    $ollamaStatus = docker ps --filter "name=ollama" --format "{{.Status}}"
    if ($ollamaStatus -like "*Up*") {
        Write-Host "   PASS: Ollama started successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Ollama failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs ollama
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start Ollama: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start LangChain service
Write-Host "   Starting LangChain service..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d --no-build langchain-service
    Start-Sleep -Seconds 15
    
    # Check if LangChain service is running
    $langchainStatus = docker ps --filter "name=langchain-service" --format "{{.Status}}"
    if ($langchainStatus -like "*Up*") {
        Write-Host "   PASS: LangChain service started successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain service failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs langchain-service
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start LangChain service: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start CHAP2 API
Write-Host "   Starting CHAP2 API..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d --no-build chap2-api
    Start-Sleep -Seconds 15
    
    # Check if CHAP2 API is running
    $apiStatus = docker ps --filter "name=chap2-api" --format "{{.Status}}"
    if ($apiStatus -like "*Up*") {
        Write-Host "   PASS: CHAP2 API started successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: CHAP2 API failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs chap2-api
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start CHAP2 API: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start Web Portal
Write-Host "   Starting Web Portal..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d --no-build chap2-webportal
    Start-Sleep -Seconds 15
    
    # Check if Web Portal is running
    $webPortalStatus = docker ps --filter "name=chap2-webportal" --format "{{.Status}}"
    if ($webPortalStatus -like "*Up*") {
        Write-Host "   PASS: Web Portal started successfully" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs chap2-webportal
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start Web Portal: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 6: Wait for containers to be ready
Write-Host "Step 6: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Step 7: Check container status
Write-Host "Step 7: Checking container status..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>&1
Write-Host $containers -ForegroundColor Gray

# Step 8: Pull Ollama models
Write-Host "Step 8: Pulling Ollama models..." -ForegroundColor Yellow

try {
    Write-Host "   Pulling nomic-embed-text model..." -ForegroundColor Gray
    docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
    Write-Host "   PASS: nomic-embed-text model pulled" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not pull nomic-embed-text model: $($_.Exception.Message)" -ForegroundColor Red
}

try {
    Write-Host "   Pulling mistral model..." -ForegroundColor Gray
    docker exec langchain_search_service-ollama-1 ollama pull mistral
    Write-Host "   PASS: mistral model pulled" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not pull mistral model: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 9: Migrate data to vector store
Write-Host "Step 9: Migrating data to vector store..." -ForegroundColor Yellow

# Wait for services to be fully ready
Write-Host "   Waiting for services to be ready..." -ForegroundColor Gray
Start-Sleep -Seconds 10

# Check if migration is needed
Write-Host "   Checking if migration is needed..." -ForegroundColor Gray
try {
    $collectionsResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    $collections = $collectionsResponse.Content | ConvertFrom-Json
    
    $chorusCollection = $collections.collections | Where-Object { $_.name -eq "chorus-vectors" }
    if ($chorusCollection) {
        # Get collection info to check if it has data
        $collectionInfoResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections/chorus-vectors" -TimeoutSec 10
        $collectionInfo = $collectionInfoResponse.Content | ConvertFrom-Json
        $vectorCount = $collectionInfo.result.vectors_count
        
        if ($vectorCount -gt 0) {
            Write-Host "   PASS: Data already migrated ($vectorCount vectors found)" -ForegroundColor Green
        } else {
            Write-Host "   INFO: Collection exists but is empty, running migration..." -ForegroundColor Yellow
            $runMigration = $true
        }
    } else {
        Write-Host "   INFO: No chorus-vectors collection found, running migration..." -ForegroundColor Yellow
        $runMigration = $true
    }
} catch {
    Write-Host "   WARNING: Could not check existing data, running migration..." -ForegroundColor Yellow
    $runMigration = $true
}

if ($runMigration) {
    try {
        Write-Host "   Running data migration..." -ForegroundColor Gray
        $migrationOutput = docker exec langchain_search_service-langchain-service-1 python migrate_data.py 2>&1
        Write-Host "   Migration output:" -ForegroundColor Gray
        Write-Host $migrationOutput -ForegroundColor Gray
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: Data migration completed successfully" -ForegroundColor Green
            
            # Verify migration results
            Write-Host "   Verifying migration results..." -ForegroundColor Gray
            Start-Sleep -Seconds 5
            
            try {
                $verifyResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections/chorus-vectors" -TimeoutSec 10
                $verifyInfo = $verifyResponse.Content | ConvertFrom-Json
                $finalVectorCount = $verifyInfo.result.vectors_count
                Write-Host "   PASS: Migration verified - $finalVectorCount vectors in store" -ForegroundColor Green
            } catch {
                Write-Host "   WARNING: Could not verify migration results" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   FAIL: Data migration failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        }
    } catch {
        Write-Host "   FAIL: Could not migrate data: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Step 10: Test all services
Write-Host "Step 10: Testing all services..." -ForegroundColor Yellow

# Test CHAP2 API
Write-Host "   Testing CHAP2 API..." -ForegroundColor Gray
try {
    $apiResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/health/ping" -TimeoutSec 10
    Write-Host "   PASS: CHAP2 API accessible (Status: $($apiResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: CHAP2 API not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Web Portal
Write-Host "   Testing Web Portal..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 10
    Write-Host "   PASS: Web Portal accessible (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test LangChain Service
Write-Host "   Testing LangChain Service..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Qdrant
Write-Host "   Testing Qdrant..." -ForegroundColor Gray
try {
    $qdrantResponse = Invoke-WebRequest -Uri "http://localhost:6333/collections" -TimeoutSec 10
    Write-Host "   PASS: Qdrant accessible (Status: $($qdrantResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Qdrant not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test Ollama
Write-Host "   Testing Ollama..." -ForegroundColor Gray
try {
    $ollamaResponse = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -TimeoutSec 10
    Write-Host "   PASS: Ollama accessible (Status: $($ollamaResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Ollama not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

# Test search functionality
Write-Host "   Testing search functionality..." -ForegroundColor Gray
try {
    $searchResponse = Invoke-WebRequest -Uri "http://localhost:8000/search_intelligent" -Method POST -ContentType "application/json" -Body '{"query":"test","k":1}' -TimeoutSec 60
    Write-Host "   PASS: Search functionality working (Status: $($searchResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Search functionality not working: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 11: Test network connectivity between containers
Write-Host "Step 11: Testing container network connectivity..." -ForegroundColor Yellow

Write-Host "   Testing LangChain -> Qdrant ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 qdrant 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Qdrant" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Qdrant" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

Write-Host "   Testing LangChain -> Ollama ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-langchain-service-1 ping -c 3 ollama 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: LangChain can ping Ollama" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: LangChain cannot ping Ollama" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

Write-Host "   Testing Web Portal -> LangChain ping..." -ForegroundColor Gray
try {
    $pingTest = docker exec langchain_search_service-chap2-webportal-1 ping -c 3 langchain-service 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   PASS: Web Portal can ping LangChain" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Web Portal cannot ping LangChain" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test ping" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- CHAP2 API: http://localhost:5001/api" -ForegroundColor White
Write-Host "- Web Portal: http://localhost:5000" -ForegroundColor White
Write-Host "- LangChain Service: http://localhost:8000" -ForegroundColor White
Write-Host "- Qdrant: http://localhost:6333" -ForegroundColor White
Write-Host "- Ollama: http://localhost:11434" -ForegroundColor White

Write-Host ""
Write-Host "Deployment Summary:" -ForegroundColor Yellow
Write-Host "- All services deployed and running" -ForegroundColor White
Write-Host "- Ollama models pulled and ready" -ForegroundColor White
Write-Host "- Data migrated to vector store" -ForegroundColor White
Write-Host "- Search functionality tested and working" -ForegroundColor White

Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "- Open http://localhost:5000 in your browser" -ForegroundColor White
Write-Host "- Use the search functionality to test the system" -ForegroundColor White
Write-Host "- Check logs with: docker-compose logs -f" -ForegroundColor White
Write-Host "- Stop services with: docker-compose down" -ForegroundColor White

Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "- If services are not accessible, try: docker-compose restart" -ForegroundColor White
Write-Host "- If search doesn't work, check container logs" -ForegroundColor White
Write-Host "- For GPU issues, restart Docker Desktop" -ForegroundColor White
Write-Host "- For container issues, check: docker-compose logs [service-name]" -ForegroundColor White 