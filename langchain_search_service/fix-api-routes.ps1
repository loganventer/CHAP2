Write-Host "========================================" -ForegroundColor Green
Write-Host "Fixing API Routes Across Applications" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# Step 1: Update Web Portal appsettings.json
Write-Host "Step 1: Updating Web Portal appsettings.json..." -ForegroundColor Yellow

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

# Step 2: Update Web Portal appsettings.Development.json
Write-Host "Step 2: Updating Web Portal appsettings.Development.json..." -ForegroundColor Yellow

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

# Step 3: Update Web Portal appsettings.Production.json
Write-Host "Step 3: Updating Web Portal appsettings.Production.json..." -ForegroundColor Yellow

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

# Step 4: Update Docker Compose environment variables
Write-Host "Step 4: Updating Docker Compose environment variables..." -ForegroundColor Yellow

$dockerComposeContent = Get-Content -Path "docker-compose.yml" -Raw
$updatedDockerCompose = $dockerComposeContent -replace 'ApiService__BaseUrl=http://chap2-api:5001', 'ApiService__BaseUrl=http://chap2-api:5001/api'

try {
    Set-Content -Path "docker-compose.yml" -Value $updatedDockerCompose
    Write-Host "   PASS: Updated docker-compose.yml" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not update docker-compose.yml: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 5: Update GPU Docker Compose files
Write-Host "Step 5: Updating GPU Docker Compose files..." -ForegroundColor Yellow

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

# Step 6: Rebuild and restart containers
Write-Host "Step 6: Rebuilding and restarting containers..." -ForegroundColor Yellow

try {
    docker-compose down
    Start-Sleep -Seconds 5
    
    docker-compose build --no-cache
    docker-compose up -d
    
    Write-Host "   PASS: Containers rebuilt and restarted" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Could not rebuild containers: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 7: Wait for containers to be ready
Write-Host "Step 7: Waiting for containers to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Step 8: Test the fixed endpoints
Write-Host "Step 8: Testing fixed endpoints..." -ForegroundColor Yellow

Write-Host "   Testing CHAP2 API /api/health/ping..." -ForegroundColor Gray
try {
    $healthResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/health/ping" -TimeoutSec 10
    Write-Host "   PASS: /api/health/ping accessible (Status: $($healthResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response: $($healthResponse.Content)" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/health/ping not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing CHAP2 API /api/choruses..." -ForegroundColor Gray
try {
    $chorusesResponse = Invoke-WebRequest -Uri "http://localhost:5001/api/choruses" -TimeoutSec 10
    Write-Host "   PASS: /api/choruses accessible (Status: $($chorusesResponse.StatusCode))" -ForegroundColor Green
    Write-Host "   Response length: $($chorusesResponse.Content.Length) characters" -ForegroundColor Gray
} catch {
    Write-Host "   FAIL: /api/choruses not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing Web Portal..." -ForegroundColor Gray
try {
    $webResponse = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 10
    Write-Host "   PASS: Web Portal accessible (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: Web Portal not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "   Testing LangChain Service..." -ForegroundColor Gray
try {
    $langchainResponse = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 10
    Write-Host "   PASS: LangChain Service accessible (Status: $($langchainResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "   FAIL: LangChain Service not accessible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "API route fixes complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

Write-Host ""
Write-Host "Fixed configurations:" -ForegroundColor Yellow
Write-Host "- Web Portal appsettings.json: ApiBaseUrl = http://chap2-api:5001" -ForegroundColor White
Write-Host "- Web Portal appsettings.Development.json: ApiBaseUrl = http://localhost:5001" -ForegroundColor White
Write-Host "- Web Portal appsettings.Production.json: ApiBaseUrl = http://chap2-api:5001" -ForegroundColor White
Write-Host "- Docker Compose: ApiService__BaseUrl = http://chap2-api:5001/api" -ForegroundColor White
Write-Host "- ChorusApiService: Already using correct /api prefix" -ForegroundColor White

Write-Host ""
Write-Host "Service URLs:" -ForegroundColor Yellow
Write-Host "- CHAP2 API: http://localhost:5001/api" -ForegroundColor White
Write-Host "- Web Portal: http://localhost:5000" -ForegroundColor White
Write-Host "- LangChain Service: http://localhost:8000" -ForegroundColor White 