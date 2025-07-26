# CHAP2 LangChain Service - Windows GPU Detection (Simplified)
# PowerShell script for deploying with automatic NVIDIA GPU detection

param(
    [switch]$ForceCPU,
    [switch]$Verbose
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CHAP2 LangChain Service - Windows GPU Detection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if command exists
function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

# Function to check Docker Desktop
function Test-DockerDesktop {
    Write-Host "Checking Docker Desktop..." -ForegroundColor Yellow
    try {
        $dockerVersion = docker version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Docker Desktop is running" -ForegroundColor Green
            return $true
        } else {
            Write-Host "ERROR: Docker Desktop is not running or not installed." -ForegroundColor Red
            Write-Host "Please start Docker Desktop and try again." -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "ERROR: Docker Desktop is not available" -ForegroundColor Red
        return $false
    }
}

# Function to check NVIDIA GPU
function Test-NvidiaGPU {
    Write-Host "Checking for NVIDIA GPU..." -ForegroundColor Yellow
    try {
        if (Test-Command "nvidia-smi") {
            $gpuInfo = nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ NVIDIA GPU detected" -ForegroundColor Green
                Write-Host ""
                Write-Host "NVIDIA GPU Information:" -ForegroundColor Cyan
                $gpuInfo | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
                Write-Host ""
                return $true
            }
        }
        Write-Host "⚠ No NVIDIA GPU detected or nvidia-smi not available" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "⚠ No NVIDIA GPU detected" -ForegroundColor Yellow
        return $false
    }
}

# Function to check NVIDIA Container Toolkit
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

# Function to create docker-compose override file
function New-DockerComposeGPU {
    param(
        [bool]$GPUAvailable,
        [bool]$ContainerGPU
    )
    
    Write-Host "Creating docker-compose.gpu.yml..." -ForegroundColor Yellow
    
    if ($GPUAvailable -and -not $ForceCPU) {
        if ($ContainerGPU) {
            Write-Host "Creating GPU-enabled configuration..." -ForegroundColor Green
            $content = @"
version: '3.8'

services:
  ollama:
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]
"@
        } else {
            Write-Host "Creating GPU configuration (NVIDIA Container Toolkit required)..." -ForegroundColor Yellow
            $content = @"
version: '3.8'

services:
  ollama:
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]

# NOTE: This configuration requires NVIDIA Container Toolkit to be installed
# If you see GPU errors, install NVIDIA Container Toolkit from:
# https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html
"@
        }
    } else {
        Write-Host "Creating CPU-only configuration..." -ForegroundColor Yellow
        $content = @"
version: '3.8'

services:
  ollama:
    # No GPU configuration - running on CPU
    # To enable GPU support, install NVIDIA drivers and Container Toolkit
"@
    }
    
    $content | Out-File -FilePath "docker-compose.gpu.yml" -Encoding UTF8
    Write-Host "✓ Configuration file created" -ForegroundColor Green
}

# Function to start services
function Start-Services {
    param(
        [bool]$GPUAvailable
    )
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Starting CHAP2 LangChain Services" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    # Stop existing containers
    Write-Host "Stopping existing containers..." -ForegroundColor Yellow
    docker-compose down 2>$null
    
    # Start Qdrant and Ollama
    Write-Host "Starting Qdrant and Ollama containers..." -ForegroundColor Yellow
    if ($GPUAvailable -and -not $ForceCPU) {
        Write-Host "Using GPU-enabled configuration" -ForegroundColor Green
        docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d qdrant ollama
    } else {
        Write-Host "Using CPU-only configuration" -ForegroundColor Yellow
        docker-compose up -d qdrant ollama
    }
    
    # Wait for Ollama to be ready
    Write-Host "Waiting for Ollama to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    # Pull required models
    Write-Host "Pulling Ollama models (this may take a while on first run)..." -ForegroundColor Yellow
    $models = @("nomic-embed-text", "mistral")
    
    foreach ($model in $models) {
        Write-Host "Pulling $model..." -ForegroundColor Yellow
        $result = docker exec langchain_search_service-ollama-1 ollama pull $model 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠ Warning: Failed to pull $model model" -ForegroundColor Yellow
            Write-Host "This may be due to network issues or insufficient disk space" -ForegroundColor Yellow
        } else {
            Write-Host "✓ $model pulled successfully" -ForegroundColor Green
        }
    }
    
    # Start LangChain service
    Write-Host "Starting LangChain service..." -ForegroundColor Yellow
    if ($GPUAvailable -and -not $ForceCPU) {
        docker-compose -f docker-compose.yml -f docker-compose.gpu.yml up -d langchain-service
    } else {
        docker-compose up -d langchain-service
    }
    
    # Wait for services to be ready
    Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
    Start-Sleep -Seconds 15
}

# Function to check service status
function Test-ServiceStatus {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Service Status" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
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

# Function to show deployment summary
function Show-DeploymentSummary {
    param(
        [bool]$GPUAvailable,
        [bool]$ContainerGPU
    )
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Deployment Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    if ($ForceCPU) {
        Write-Host "ℹ CPU-only deployment (forced)" -ForegroundColor Yellow
        Write-Host "  - GPU support disabled by -ForceCPU parameter" -ForegroundColor Gray
        Write-Host "  - Ollama running on CPU" -ForegroundColor Gray
    } elseif ($GPUAvailable) {
        if ($ContainerGPU) {
            Write-Host "✓ GPU-accelerated deployment successful" -ForegroundColor Green
            Write-Host "  - NVIDIA GPU detected and enabled" -ForegroundColor Gray
            Write-Host "  - NVIDIA Container Toolkit available" -ForegroundColor Gray
            Write-Host "  - Ollama running with GPU acceleration" -ForegroundColor Gray
        } else {
            Write-Host "⚠ GPU detected but Container Toolkit not available" -ForegroundColor Yellow
            Write-Host "  - NVIDIA GPU detected" -ForegroundColor Gray
            Write-Host "  - Install NVIDIA Container Toolkit for GPU acceleration" -ForegroundColor Gray
            Write-Host "  - Currently running on CPU" -ForegroundColor Gray
        }
    } else {
        Write-Host "ℹ CPU-only deployment successful" -ForegroundColor Yellow
        Write-Host "  - No NVIDIA GPU detected" -ForegroundColor Gray
        Write-Host "  - Ollama running on CPU" -ForegroundColor Gray
        Write-Host "  - Performance may be slower than GPU version" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Services are ready:" -ForegroundColor Cyan
    Write-Host "- Qdrant Vector Database: http://localhost:6333" -ForegroundColor White
    Write-Host "- Ollama LLM Service: http://localhost:11434" -ForegroundColor White
    Write-Host "- LangChain Search Service: http://localhost:8000" -ForegroundColor White
    Write-Host ""
    Write-Host "To start the web portal, run:" -ForegroundColor Cyan
    Write-Host "dotnet run --project CHAP2.UI/CHAP2.WebPortal/CHAP2.Web.csproj --urls `"http://localhost:5000`"" -ForegroundColor White
}

# Main execution
try {
    # Check Docker Desktop
    if (-not (Test-DockerDesktop)) {
        exit 1
    }
    
    # Check GPU and Container Toolkit
    $gpuAvailable = Test-NvidiaGPU
    $containerGPU = Test-NvidiaContainerToolkit
    
    # Create configuration
    New-DockerComposeGPU -GPUAvailable $gpuAvailable -ContainerGPU $containerGPU
    
    # Start services
    Start-Services -GPUAvailable $gpuAvailable
    
    # Check service status
    Test-ServiceStatus
    
    # Show summary
    Show-DeploymentSummary -GPUAvailable $gpuAvailable -ContainerGPU $containerGPU
    
} catch {
    Write-Host "ERROR: An unexpected error occurred: $($_.Exception.Message)" -ForegroundColor Red
    if ($Verbose) {
        Write-Host $_.Exception.StackTrace -ForegroundColor Red
    }
    exit 1
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 