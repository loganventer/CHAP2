# CHAP2 LangChain Service - Clean Start
param([switch]$ForceCPU, [switch]$Verbose)

Write-Host "CHAP2 LangChain Service - Windows GPU Detection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

function Test-DockerDesktop {
    Write-Host "Checking Docker Desktop..." -ForegroundColor Yellow
    try {
        $dockerVersion = docker version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Docker Desktop is running" -ForegroundColor Green
            return $true
        } else {
            Write-Host "ERROR: Docker Desktop is not running" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "ERROR: Docker Desktop is not available" -ForegroundColor Red
        return $false
    }
}

function Test-NvidiaGPU {
    Write-Host "Checking for NVIDIA GPU..." -ForegroundColor Yellow
    try {
        if (Test-Command "nvidia-smi") {
            $gpuInfo = nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ NVIDIA GPU detected" -ForegroundColor Green
                return $true
            }
        }
        Write-Host "⚠ No NVIDIA GPU detected" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "⚠ No NVIDIA GPU detected" -ForegroundColor Yellow
        return $false
    }
}

function Test-NvidiaContainerToolkit {
    Write-Host "Checking NVIDIA Container Toolkit..." -ForegroundColor Yellow
    try {
        $testResult = docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ NVIDIA Container Toolkit is available" -ForegroundColor Green
            return $true
        } else {
            Write-Host "⚠ NVIDIA Container Toolkit not available" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "⚠ NVIDIA Container Toolkit not available" -ForegroundColor Yellow
        return $false
    }
}

function New-DockerComposeGPU {
    param([bool]$GPUAvailable, [bool]$ContainerGPU)
    
    Write-Host "Creating docker-compose.gpu.yml..." -ForegroundColor Yellow
    
    if ($GPUAvailable -and -not $ForceCPU) {
        if ($ContainerGPU) {
            Write-Host "Creating GPU-enabled configuration..." -ForegroundColor Green
            $content = "version: '3.8'`n`nservices:`n  ollama:`n    deploy:`n      resources:`n        reservations:`n          devices:`n            - driver: nvidia`n              count: all`n              capabilities: [gpu]"
        } else {
            Write-Host "Creating GPU configuration..." -ForegroundColor Yellow
            $content = "version: '3.8'`n`nservices:`n  ollama:`n    deploy:`n      resources:`n        reservations:`n          devices:`n            - driver: nvidia`n              count: all`n              capabilities: [gpu]"
        }
    } else {
        Write-Host "Creating CPU-only configuration..." -ForegroundColor Yellow
        $content = "version: '3.8'`n`nservices:`n  ollama:`n    # No GPU configuration"
    }
    
    $content | Out-File -FilePath "docker-compose.gpu.yml" -Encoding UTF8
    Write-Host "✓ Configuration file created" -ForegroundColor Green
}

function Start-Services {
    param([bool]$GPUAvailable)
    
    Write-Host "Starting CHAP2 LangChain Services" -ForegroundColor Cyan
    
    Write-Host "Stopping existing containers..." -ForegroundColor Yellow
    docker-compose down 2>$null
    
    Write-Host "Starting Qdrant and Ollama containers..." -ForegroundColor Yellow
    if ($GPUAvailable -and -not $ForceCPU) {
        Write-Host "Using GPU-enabled configuration" -ForegroundColor Green
        docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d qdrant ollama
    } else {
        Write-Host "Using CPU-only configuration" -ForegroundColor Yellow
        docker-compose up -d qdrant ollama
    }
    
    Write-Host "Waiting for Ollama to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    Write-Host "Pulling Ollama models..." -ForegroundColor Yellow
    docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text 2>$null
    docker exec langchain_search_service-ollama-1 ollama pull mistral 2>$null
    
    Write-Host "Starting LangChain service..." -ForegroundColor Yellow
    if ($GPUAvailable -and -not $ForceCPU) {
        docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d langchain-service
    } else {
        docker-compose up -d langchain-service
    }
    
    Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
}

function Test-ServiceStatus {
    Write-Host "Service Status" -ForegroundColor Cyan
    
    $services = @(
        @{Name="Qdrant"; URL="http://localhost:6333/collections"},
        @{Name="Ollama"; URL="http://localhost:11434/api/tags"},
        @{Name="LangChain service"; URL="http://localhost:8000/health"}
    )
    
    foreach ($service in $services) {
        Write-Host "Checking $($service.Name)..." -ForegroundColor Yellow
        try {
            $response = Invoke-WebRequest -Uri $service.URL -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "✓ $($service.Name) is running" -ForegroundColor Green
            } else {
                Write-Host "✗ $($service.Name) is not responding" -ForegroundColor Red
            }
        } catch {
            Write-Host "✗ $($service.Name) is not responding" -ForegroundColor Red
        }
    }
}

function Show-DeploymentSummary {
    param([bool]$GPUAvailable, [bool]$ContainerGPU)
    
    Write-Host "Deployment Summary" -ForegroundColor Cyan
    
    if ($ForceCPU) {
        Write-Host "ℹ CPU-only deployment (forced)" -ForegroundColor Yellow
    } elseif ($GPUAvailable) {
        if ($ContainerGPU) {
            Write-Host "✓ GPU-accelerated deployment successful" -ForegroundColor Green
        } else {
            Write-Host "⚠ GPU detected but Container Toolkit not available" -ForegroundColor Yellow
        }
    } else {
        Write-Host "ℹ CPU-only deployment successful" -ForegroundColor Yellow
    }
    
    Write-Host "Services are ready:" -ForegroundColor Cyan
    Write-Host "- Qdrant Vector Database: http://localhost:6333" -ForegroundColor White
    Write-Host "- Ollama LLM Service: http://localhost:11434" -ForegroundColor White
    Write-Host "- LangChain Search Service: http://localhost:8000" -ForegroundColor White
}

try {
    if (-not (Test-DockerDesktop)) {
        exit 1
    }
    
    $gpuAvailable = Test-NvidiaGPU
    $containerGPU = Test-NvidiaContainerToolkit
    
    New-DockerComposeGPU -GPUAvailable $gpuAvailable -ContainerGPU $containerGPU
    Start-Services -GPUAvailable $gpuAvailable
    Test-ServiceStatus
    Show-DeploymentSummary -GPUAvailable $gpuAvailable -ContainerGPU $containerGPU
    
} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 