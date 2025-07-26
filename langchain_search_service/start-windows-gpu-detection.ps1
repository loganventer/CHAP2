# CHAP2 LangChain Service - Windows GPU Detection
# PowerShell script for deploying with automatic NVIDIA GPU detection and installation

param(
    [switch]$ForceCPU,
    [switch]$Verbose,
    [switch]$AutoInstall,
    [switch]$SkipPrompts
)

# Set error action preference
$ErrorActionPreference = "Continue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "CHAP2 LangChain Service - Windows GPU Detection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if command exists
function Test-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
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

# Function to check NVIDIA drivers
function Test-NvidiaDrivers {
    Write-Host "Checking NVIDIA drivers..." -ForegroundColor Yellow
    try {
        if (Test-Command "nvidia-smi") {
            $driverInfo = nvidia-smi --query-gpu=driver_version --format=csv,noheader 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ NVIDIA drivers are installed" -ForegroundColor Green
                return $true
            }
        }
        Write-Host "⚠ NVIDIA drivers not detected" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "⚠ NVIDIA drivers not detected" -ForegroundColor Yellow
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

# Function to install NVIDIA Container Toolkit
function Install-NvidiaContainerToolkit {
    Write-Host "Installing NVIDIA Container Toolkit..." -ForegroundColor Yellow
    
    # Check if running as administrator
    if (-not (Test-Administrator)) {
        Write-Host "ERROR: Administrator privileges required to install NVIDIA Container Toolkit" -ForegroundColor Red
        Write-Host "Please run this script as Administrator" -ForegroundColor Red
        return $false
    }
    
    try {
        # Download and install NVIDIA Container Toolkit
        Write-Host "Downloading NVIDIA Container Toolkit installer..." -ForegroundColor Yellow
        
        # Get the latest version from NVIDIA
        $downloadUrl = "https://nvidia.github.io/libnvidia-container/windows/nvidia-container-toolkit-windows-latest.exe"
        $installerPath = "$env:TEMP\nvidia-container-toolkit-installer.exe"
        
        # Download installer
        Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
        
        if (Test-Path $installerPath) {
            Write-Host "Installing NVIDIA Container Toolkit..." -ForegroundColor Yellow
            Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait
            
            # Clean up installer
            Remove-Item $installerPath -Force
            
            Write-Host "✓ NVIDIA Container Toolkit installed successfully" -ForegroundColor Green
            return $true
        } else {
            Write-Host "ERROR: Failed to download NVIDIA Container Toolkit installer" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "ERROR: Failed to install NVIDIA Container Toolkit" -ForegroundColor Red
        Write-Host "Please install manually from: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html" -ForegroundColor Yellow
        return $false
    }
}

# Function to configure Docker Desktop for GPU
function Configure-DockerDesktopGPU {
    Write-Host "Configuring Docker Desktop for GPU support..." -ForegroundColor Yellow
    
    try {
        # Check if Docker Desktop settings can be configured
        $dockerSettingsPath = "$env:APPDATA\Docker\settings.json"
        
        if (Test-Path $dockerSettingsPath) {
            $settings = Get-Content $dockerSettingsPath | ConvertFrom-Json
            
            # Enable GPU acceleration if not already enabled
            if (-not $settings.gpuAcceleration) {
                $settings.gpuAcceleration = $true
                $settings | ConvertTo-Json -Depth 10 | Set-Content $dockerSettingsPath
                Write-Host "✓ GPU acceleration enabled in Docker Desktop" -ForegroundColor Green
            } else {
                Write-Host "✓ GPU acceleration already enabled in Docker Desktop" -ForegroundColor Green
            }
            
            # Enable WSL 2 if not already enabled
            if (-not $settings.useWsl2Engine) {
                $settings.useWsl2Engine = $true
                $settings | ConvertTo-Json -Depth 10 | Set-Content $dockerSettingsPath
                Write-Host "✓ WSL 2 engine enabled in Docker Desktop" -ForegroundColor Green
            } else {
                Write-Host "✓ WSL 2 engine already enabled in Docker Desktop" -ForegroundColor Green
            }
            
            return $true
        } else {
            Write-Host "⚠ Docker Desktop settings file not found" -ForegroundColor Yellow
            Write-Host "Please manually enable GPU acceleration in Docker Desktop settings" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "⚠ Failed to configure Docker Desktop automatically" -ForegroundColor Yellow
        Write-Host "Please manually enable GPU acceleration in Docker Desktop settings" -ForegroundColor Yellow
        return $false
    }
}

# Function to prompt for installation
function Prompt-Installation {
    param(
        [string]$Component,
        [string]$Description,
        [string]$ManualUrl
    )
    
    if ($AutoInstall -or $SkipPrompts) {
        return $true
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Installation Required" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Component: $Component" -ForegroundColor White
    Write-Host "Description: $Description" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "1. Auto-install (requires admin privileges)" -ForegroundColor White
    Write-Host "2. Manual installation" -ForegroundColor White
    Write-Host "3. Skip installation" -ForegroundColor White
    Write-Host ""
    
    do {
        $choice = Read-Host "Enter your choice (1-3)"
        switch ($choice) {
            "1" { return $true }
            "2" { 
                Write-Host "Manual installation URL: $ManualUrl" -ForegroundColor Cyan
                Start-Process $ManualUrl
                return $false
            }
            "3" { return $false }
            default { Write-Host "Invalid choice. Please enter 1, 2, or 3." -ForegroundColor Red }
        }
    } while ($true)
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
            $content = "version: '3.8'`n`nservices:`n  ollama:`n    deploy:`n      resources:`n        reservations:`n          devices:`n            - driver: nvidia`n              count: all`n              capabilities: [gpu]"
        } else {
            Write-Host "Creating GPU configuration (NVIDIA Container Toolkit required)..." -ForegroundColor Yellow
            $content = "version: '3.8'`n`nservices:`n  ollama:`n    deploy:`n      resources:`n        reservations:`n          devices:`n            - driver: nvidia`n              count: all`n              capabilities: [gpu]`n`n# NOTE: This configuration requires NVIDIA Container Toolkit to be installed`n# If you see GPU errors, install NVIDIA Container Toolkit from:`n# https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html"
        }
    } else {
        Write-Host "Creating CPU-only configuration..." -ForegroundColor Yellow
        $content = "version: '3.8'`n`nservices:`n  ollama:`n    # No GPU configuration - running on CPU`n    # To enable GPU support, install NVIDIA drivers and Container Toolkit"
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
    
    # Check GPU and drivers
    $gpuAvailable = Test-NvidiaGPU
    $driversInstalled = Test-NvidiaDrivers
    $containerGPU = Test-NvidiaContainerToolkit
    
    # Handle GPU setup if detected
    if ($gpuAvailable -and -not $ForceCPU) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "NVIDIA GPU Detected - Setting up GPU Support" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        
        # Check and install drivers if needed
        if (-not $driversInstalled) {
            Write-Host "NVIDIA drivers not detected" -ForegroundColor Yellow
            if (Prompt-Installation -Component "NVIDIA Drivers" -Description "Required for GPU acceleration" -ManualUrl "https://www.nvidia.com/Download/index.aspx") {
                Write-Host "Please install NVIDIA drivers manually and restart this script" -ForegroundColor Yellow
                exit 1
            }
        }
        
        # Configure Docker Desktop
        Configure-DockerDesktopGPU
        
        # Check and install Container Toolkit if needed
        if (-not $containerGPU) {
            Write-Host "NVIDIA Container Toolkit not detected" -ForegroundColor Yellow
            if (Prompt-Installation -Component "NVIDIA Container Toolkit" -Description "Required for GPU acceleration in Docker" -ManualUrl "https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html") {
                if (Install-NvidiaContainerToolkit) {
                    Write-Host "Restarting Docker Desktop to apply changes..." -ForegroundColor Yellow
                    # Note: Docker Desktop restart would require user intervention
                    Write-Host "Please restart Docker Desktop manually and run this script again" -ForegroundColor Yellow
                    exit 0
                }
            }
        }
        
        # Re-check Container Toolkit after potential installation
        $containerGPU = Test-NvidiaContainerToolkit
    }
    
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