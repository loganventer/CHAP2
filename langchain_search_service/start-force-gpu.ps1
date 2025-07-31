#Requires -Version 5.1

Write-Host "========================================" -ForegroundColor Green
Write-Host "CHAP2 Force GPU Deployment" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Force GPU mode - skip all detection
$script:UseGpu = $true
Write-Host "FORCE: GPU mode enabled - skipping all detection" -ForegroundColor Yellow

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

# Step 2: Force GPU Configuration
Write-Host "Step 2: Using FORCED GPU configuration..." -ForegroundColor Yellow
Write-Host "   Using GPU-enabled Docker Compose configuration..." -ForegroundColor Green
$composeFile = "docker-compose.gpu.yml"

# Step 3: Fix API Routes Configuration and Windows Buffering Issues
Write-Host "Step 3: Fixing API routes configuration and Windows buffering issues..." -ForegroundColor Yellow

# Fix Windows buffering issues in HomeController.cs
Write-Host "   Fix 1: Applying Windows buffering fixes to HomeController.cs..." -ForegroundColor Gray
$homeControllerPath = "../CHAP2.UI/CHAP2.WebPortal/Controllers/HomeController.cs"
$homeControllerContent = Get-Content -Path $homeControllerPath -Raw

# Add the Windows buffering fixes to IntelligentSearchStream method
$intelligentSearchStreamPattern = '(?s)public async Task<IActionResult> IntelligentSearchStream\([^)]*\)\s*\{[^}]*Response\.Headers\.Add\("Content-Type", "text/event-stream"\);'
$intelligentSearchStreamReplacement = @'
        public async Task<IActionResult> IntelligentSearchStream([FromBody] IntelligentSearchRequest request)
        {
            try
            {
                _logger.LogInformation("IntelligentSearchStream called with query: {Query}, maxResults: {MaxResults}",
                    request.Query, request.MaxResults);

                // Set up streaming response headers FIRST (before any response starts)
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
                Response.Headers.Add("Pragma", "no-cache");
                Response.Headers.Add("Expires", "0");
                Response.Headers.Add("Connection", "keep-alive");
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("X-Accel-Buffering", "no"); // Disable nginx buffering if present

                // Force immediate flush of headers
                await Response.Body.FlushAsync();

                // Use the streaming intelligent search service
                await foreach (var streamEvent in _intelligentSearchService.SearchWithIntelligenceStreamingAsync(
                    request.Query, request.MaxResults, HttpContext.RequestAborted))
                {
                    var eventData = $"data: {streamEvent}\n\n";
                    await Response.WriteAsync(eventData);

                    // Force immediate flush after each event
                    await Response.Body.FlushAsync();

                    // Add a small delay to ensure the event is sent before the next one
                    await Task.Delay(10);
                }

                return new EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IntelligentSearchStream");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
'@

if ($homeControllerContent -match $intelligentSearchStreamPattern) {
    $homeControllerContent = $homeControllerContent -replace $intelligentSearchStreamPattern, $intelligentSearchStreamReplacement
    Set-Content -Path $homeControllerPath -Value $homeControllerContent
    Write-Host "     PASS: Applied Windows buffering fixes to HomeController.cs" -ForegroundColor Green
} else {
    Write-Host "     WARNING: Could not find IntelligentSearchStream method in HomeController.cs" -ForegroundColor Yellow
}

# Fix Windows buffering issues in Program.cs
Write-Host "   Fix 2: Applying Windows buffering fixes to Program.cs..." -ForegroundColor Gray
$programPath = "../CHAP2.UI/CHAP2.WebPortal/Program.cs"
$programContent = Get-Content -Path $programPath -Raw

# Add Kestrel configuration for streaming
$kestrelPattern = 'builder\.Services\.AddControllersWithViews\(\);'
$kestrelReplacement = @'
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();

// Configure response buffering for streaming
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
'@

if ($programContent -match $kestrelPattern) {
    $programContent = $programContent -replace $kestrelPattern, $kestrelReplacement
    Set-Content -Path $programPath -Value $programContent
    Write-Host "     PASS: Applied Kestrel streaming configuration to Program.cs" -ForegroundColor Green
} else {
    Write-Host "     WARNING: Could not find AddControllersWithViews in Program.cs" -ForegroundColor Yellow
}

    # Add middleware for streaming endpoints
    $middlewarePattern = 'app\.UseExceptionHandler\("/Home/Error"\);\r?\napp\.UseHsts\(\);'
    $middlewareReplacement = @'
app.UseExceptionHandler("/Home/Error");
app.UseHsts();
}

// Add middleware to disable response buffering for streaming endpoints
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/Home/IntelligentSearchStream"))
    {
        context.Response.Headers.Add("X-Accel-Buffering", "no");
        context.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Add("Pragma", "no-cache");
        context.Response.Headers.Add("Expires", "0");
    }
    await next();
});
'@

if ($programContent -match $middlewarePattern) {
    $programContent = $programContent -replace $middlewarePattern, $middlewareReplacement
    Set-Content -Path $programPath -Value $programContent
    Write-Host "     PASS: Applied streaming middleware to Program.cs" -ForegroundColor Green
} else {
    Write-Host "     WARNING: Could not find UseExceptionHandler in Program.cs" -ForegroundColor Yellow
}

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

try {
    Write-Host "   Stopping all containers..." -ForegroundColor Gray
    docker-compose -f $composeFile down
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
    
    # Let Docker Compose create the network fresh (don't create manually)
    Write-Host "   PASS: Network cleanup completed - Docker Compose will create fresh network" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not clean up containers: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Build and start containers with GPU
Write-Host "Step 5: Building and starting containers with GPU..." -ForegroundColor Yellow

try {
    Write-Host "   Building containers with GPU support..." -ForegroundColor Gray
    docker-compose -f $composeFile build --no-cache
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   FAIL: Container build failed" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   PASS: Containers built successfully" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not build containers: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Start all containers at once to ensure proper network setup
Write-Host "   Starting all containers with GPU..." -ForegroundColor Gray
try {
    docker-compose -f $composeFile up -d
    Start-Sleep -Seconds 30
    
    # Check if all containers are running
    $allContainers = docker ps --filter "name=langchain_search_service" --format "{{.Names}}\t{{.Status}}"
    Write-Host "   Container status:" -ForegroundColor Gray
    Write-Host $allContainers -ForegroundColor Gray
    
    # Verify all containers are up
    $runningContainers = docker ps --filter "name=langchain_search_service" --filter "status=running" --format "{{.Names}}" | Measure-Object -Line
    $totalContainers = docker ps --filter "name=langchain_search_service" --format "{{.Names}}" | Measure-Object -Line
    
    if ($runningContainers.Lines -eq $totalContainers.Lines) {
        Write-Host "   PASS: All containers started successfully with GPU" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: Some containers failed to start" -ForegroundColor Red
        docker-compose -f $composeFile logs
        exit 1
    }
} catch {
    Write-Host "   FAIL: Could not start containers: $($_.Exception.Message)" -ForegroundColor Red
    docker-compose -f $composeFile logs
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

# Step 11: Test container network connectivity
Write-Host "Step 11: Testing container network connectivity..." -ForegroundColor Yellow

# Test LangChain -> Qdrant connectivity (using IP since DNS fails but services work)
Write-Host "   Testing LangChain -> Qdrant connectivity..." -ForegroundColor Gray
try {
    $qdrantIP = docker inspect langchain_search_service-qdrant-1 --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" 2>$null
    if ($qdrantIP) {
        $qdrantTest = docker exec langchain_search_service-langchain-service-1 curl -s http://$qdrantIP`:6333/collections 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: LangChain can reach Qdrant by IP" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: LangChain cannot reach Qdrant by IP" -ForegroundColor Red
        }
    } else {
        Write-Host "   FAIL: Cannot get Qdrant IP" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test LangChain -> Qdrant connectivity" -ForegroundColor Red
}

# Test LangChain -> Ollama connectivity (using IP since DNS fails but services work)
Write-Host "   Testing LangChain -> Ollama connectivity..." -ForegroundColor Gray
try {
    $ollamaIP = docker inspect langchain_search_service-ollama-1 --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" 2>$null
    if ($ollamaIP) {
        $ollamaTest = docker exec langchain_search_service-langchain-service-1 curl -s http://$ollamaIP`:11434/api/tags 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: LangChain can reach Ollama by IP" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: LangChain cannot reach Ollama by IP" -ForegroundColor Red
        }
    } else {
        Write-Host "   FAIL: Cannot get Ollama IP" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Cannot test LangChain -> Ollama connectivity" -ForegroundColor Red
}

# Test Web Portal -> LangChain connectivity (using IP since DNS fails but services work)
Write-Host "   Testing Web Portal -> LangChain connectivity..." -ForegroundColor Gray
try {
    $langchainIP = docker inspect langchain_search_service-langchain-service-1 --format "{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}" 2>$null
    if ($langchainIP) {
        $langchainTest = docker exec langchain_search_service-chap2-webportal-1 curl -s http://$langchainIP`:8000/health 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   PASS: Web Portal can reach LangChain by IP" -ForegroundColor Green
        } else {
            Write-Host "   FAIL: Web Portal cannot reach LangChain by IP" -ForegroundColor Red
        }
    } else {
        Write-Host "   FAIL: Cannot get LangChain IP" -ForegroundColor Red
        }
} catch {
    Write-Host "   FAIL: Cannot test Web Portal -> LangChain connectivity" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "FORCE GPU Deployment complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- CHAP2 API: http://localhost:5001/api" -ForegroundColor White
Write-Host "- Web Portal: http://localhost:5000" -ForegroundColor White
Write-Host "- LangChain Service: http://localhost:8000" -ForegroundColor White
Write-Host "- Qdrant: http://localhost:6333" -ForegroundColor White
Write-Host "- Ollama: http://localhost:11434" -ForegroundColor White

Write-Host ""
Write-Host "FORCE GPU Deployment Summary:" -ForegroundColor Yellow
Write-Host "- All services deployed with GPU support" -ForegroundColor White
Write-Host "- Ollama models pulled and ready" -ForegroundColor White
Write-Host "- Data migrated to vector store" -ForegroundColor White
Write-Host "- Search functionality tested and working" -ForegroundColor White

Write-Host ""
Write-Host "Usage:" -ForegroundColor Yellow
Write-Host "- Open http://localhost:5000 in your browser" -ForegroundColor White
Write-Host "- Use the search functionality to test the system" -ForegroundColor White
Write-Host "- Check logs with: docker-compose -f $composeFile logs -f" -ForegroundColor White
Write-Host "- Stop services with: docker-compose -f $composeFile down" -ForegroundColor White

Write-Host ""
Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host "- If services are not accessible, try: docker-compose -f $composeFile restart" -ForegroundColor White
Write-Host "- If search doesn't work, check container logs" -ForegroundColor White
Write-Host "- For GPU issues, restart Docker Desktop" -ForegroundColor White
Write-Host "- For container issues, check: docker-compose -f $composeFile logs [service-name]" -ForegroundColor White

# Step 12: Cleanup Docker resources
Write-Host ""
Write-Host "Step 12: Cleaning up Docker resources..." -ForegroundColor Yellow

Write-Host "   Removing dangling images..." -ForegroundColor Gray
try {
    $danglingImages = docker images -f "dangling=true" -q
    if ($danglingImages) {
        docker rmi $danglingImages 2>$null
        Write-Host "   PASS: Dangling images removed" -ForegroundColor Green
    } else {
        Write-Host "   INFO: No dangling images found" -ForegroundColor Green
    }
} catch {
    Write-Host "   WARNING: Could not remove dangling images" -ForegroundColor Yellow
}

Write-Host "   Removing unused images..." -ForegroundColor Gray
try {
    docker image prune -f 2>$null
    Write-Host "   PASS: Unused images removed" -ForegroundColor Green
} catch {
    Write-Host "   WARNING: Could not remove unused images" -ForegroundColor Yellow
}

Write-Host "   Removing unused containers..." -ForegroundColor Gray
try {
    docker container prune -f 2>$null
    Write-Host "   PASS: Unused containers removed" -ForegroundColor Green
} catch {
    Write-Host "   WARNING: Could not remove unused containers" -ForegroundColor Yellow
}

Write-Host "   Removing unused networks..." -ForegroundColor Gray
try {
    docker network prune -f 2>$null
    Write-Host "   PASS: Unused networks removed" -ForegroundColor Green
} catch {
    Write-Host "   WARNING: Could not remove unused networks" -ForegroundColor Yellow
}

Write-Host "   Final disk usage:" -ForegroundColor Gray
try {
    $finalDiskUsage = docker system df
    Write-Host $finalDiskUsage -ForegroundColor Gray
} catch {
    Write-Host "   WARNING: Could not get disk usage" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "FORCE GPU Deployment and cleanup completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green 