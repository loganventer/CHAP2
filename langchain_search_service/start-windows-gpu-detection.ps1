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

Write-Host "CHAP2 LangChain Service - Windows GPU Detection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

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
            Write-Host "Docker Desktop is running" -ForegroundColor Green
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

# Function to check NVIDIA GPU
function Test-NvidiaGPU {
    Write-Host "Checking for NVIDIA GPU..." -ForegroundColor Yellow
    try {
        if (Test-Command "nvidia-smi") {
            $gpuInfo = nvidia-smi --query-gpu=name,memory.total,driver_version --format=csv,noheader,nounits 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "NVIDIA GPU detected" -ForegroundColor Green
                return $true
            }
        }
        Write-Host "No NVIDIA GPU detected" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "No NVIDIA GPU detected" -ForegroundColor Yellow
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
                Write-Host "NVIDIA drivers are installed" -ForegroundColor Green
                return $true
            }
        }
        Write-Host "NVIDIA drivers not detected" -ForegroundColor Yellow
        return $false
    } catch {
        Write-Host "NVIDIA drivers not detected" -ForegroundColor Yellow
        return $false
    }
}

# Function to check NVIDIA Container Toolkit
function Test-NvidiaContainerToolkit {
    Write-Host "Checking NVIDIA Container Toolkit..." -ForegroundColor Yellow
    try {
        $job = Start-Job -ScriptBlock {
            docker run --rm --gpus all nvidia/cuda:11.0-base nvidia-smi 2>$null
            return $LASTEXITCODE
        }
        
        if (Wait-Job $job -Timeout 30) {
            $result = Receive-Job $job
            Remove-Job $job
            if ($result -eq 0) {
                Write-Host "NVIDIA Container Toolkit is available" -ForegroundColor Green
                return $true
            } else {
                Write-Host "NVIDIA Container Toolkit not available" -ForegroundColor Yellow
                return $false
            }
        } else {
            Stop-Job $job
            Remove-Job $job
            Write-Host "NVIDIA Container Toolkit check timed out" -ForegroundColor Yellow
            return $false
        }
    } catch {
        Write-Host "NVIDIA Container Toolkit not available" -ForegroundColor Yellow
        return $false
    }
}

# Function to install NVIDIA Container Toolkit
function Install-NvidiaContainerToolkit {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Installing NVIDIA Container Toolkit" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    # Check if running as administrator
    if (-not (Test-Administrator)) {
        Write-Host "ERROR: Administrator privileges required" -ForegroundColor Red
        Write-Host "   Please run this script as Administrator" -ForegroundColor Red
        Write-Host "   Right-click the script and select 'Run as Administrator'" -ForegroundColor Gray
        return $false
    }
    
    Write-Host "Administrator privileges confirmed" -ForegroundColor Green
    
    try {
        # Step 1: Download installer
        Write-Host ""
        Write-Host "Step 1: Downloading installer..." -ForegroundColor Yellow
        Write-Host "   Source: NVIDIA Container Toolkit for Windows" -ForegroundColor Gray
        Write-Host "   Size: ~50MB (may take a few minutes)" -ForegroundColor Gray
        Write-Host "   URL: https://github.com/NVIDIA/nvidia-container-toolkit/releases" -ForegroundColor Gray
        
        # Get the latest release URL
        $downloadUrl = "https://github.com/NVIDIA/nvidia-container-toolkit/releases/latest/download/nvidia-container-toolkit-windows-amd64.exe"
        $installerPath = "$env:TEMP\nvidia-container-toolkit-installer.exe"
        
        Write-Host "   Starting download..." -ForegroundColor Yellow
        
        # Download the installer with better error handling
        try {
            Write-Host "   Downloading installer..." -ForegroundColor Yellow
            Write-Host "   Note: If download fails, please download manually from:" -ForegroundColor Gray
            Write-Host "   https://github.com/NVIDIA/nvidia-container-toolkit/releases" -ForegroundColor Gray
            
            # Set timeout and retry settings
            $webClient = New-Object System.Net.WebClient
            $webClient.Timeout = 300000  # 5 minutes timeout
            $webClient.DownloadFile($downloadUrl, $installerPath)
            
            Write-Host "   Download completed!" -ForegroundColor Green
        } catch {
            Write-Host "   Download failed: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "   This could be due to:" -ForegroundColor Yellow
            Write-Host "   - Network connectivity issues" -ForegroundColor Gray
            Write-Host "   - Firewall blocking the download" -ForegroundColor Gray
            Write-Host "   - GitHub rate limiting" -ForegroundColor Gray
            Write-Host "   " -ForegroundColor White
            Write-Host "   Manual download required:" -ForegroundColor Yellow
            Write-Host "   1. Visit: https://github.com/NVIDIA/nvidia-container-toolkit/releases" -ForegroundColor Gray
            Write-Host "   2. Download the latest Windows AMD64 installer" -ForegroundColor Gray
            Write-Host "   3. Run the installer manually" -ForegroundColor Gray
            Write-Host "   4. Restart this script after installation" -ForegroundColor Gray
            throw
        }
        
        if (Test-Path $installerPath) {
            $fileSize = (Get-Item $installerPath).Length / 1MB
            Write-Host "Download completed ($([math]::Round($fileSize, 1)) MB)" -ForegroundColor Green
        } else {
            Write-Host "Download failed" -ForegroundColor Red
            return $false
        }
        
        # Step 2: Install
        Write-Host ""
        Write-Host "Step 2: Installing NVIDIA Container Toolkit..." -ForegroundColor Yellow
        Write-Host "   This may take 1-2 minutes" -ForegroundColor Gray
        Write-Host "   Please wait for installation to complete..." -ForegroundColor Gray
        
        $startTime = Get-Date
        $process = Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait -PassThru
        
        $duration = (Get-Date) - $startTime
        Write-Host "Installation completed in $($duration.TotalSeconds.ToString('F1')) seconds" -ForegroundColor Green
        
        # Step 3: Cleanup
        Write-Host ""
        Write-Host "Step 3: Cleaning up..." -ForegroundColor Yellow
        Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        Write-Host "Temporary files cleaned up" -ForegroundColor Green
        
        # Step 4: Verify installation
        Write-Host ""
        Write-Host "Step 4: Verifying installation..." -ForegroundColor Yellow
        
        # Wait a moment for installation to settle
        Start-Sleep -Seconds 3
        
        # Test if the installation was successful
        $testResult = Test-NvidiaContainerToolkit
        if ($testResult) {
            Write-Host "NVIDIA Container Toolkit verified successfully" -ForegroundColor Green
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "Installation Complete!" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "NVIDIA Container Toolkit is now ready for use" -ForegroundColor White
            return $true
        } else {
            Write-Host "Installation may need Docker Desktop restart" -ForegroundColor Yellow
            Write-Host "   Please restart Docker Desktop and run this script again" -ForegroundColor Yellow
            return $false
        }
        
    } catch {
        Write-Host ""
        Write-Host "ERROR: Installation failed" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Manual installation required:" -ForegroundColor Yellow
        Write-Host "1. Visit: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html" -ForegroundColor Gray
        Write-Host "2. Download and install NVIDIA Container Toolkit for Windows" -ForegroundColor Gray
        Write-Host "3. Restart Docker Desktop" -ForegroundColor Gray
        Write-Host "4. Run this script again" -ForegroundColor Gray
        return $false
    }
}

# Function to configure Docker Desktop for GPU
function Configure-DockerDesktopGPU {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Configuring Docker Desktop for GPU" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    
    try {
        # Check if Docker Desktop settings can be configured
        $dockerSettingsPath = "$env:APPDATA\Docker\settings.json"
        
        if (Test-Path $dockerSettingsPath) {
            Write-Host "Docker Desktop settings file found" -ForegroundColor Green
            $settings = Get-Content $dockerSettingsPath | ConvertFrom-Json
            $modified = $false
            
            # Check and enable GPU acceleration
            Write-Host ""
            Write-Host "Checking GPU acceleration setting..." -ForegroundColor Yellow
            if (-not $settings.gpuAcceleration) {
                $settings.gpuAcceleration = $true
                $modified = $true
                Write-Host "GPU acceleration enabled in Docker Desktop" -ForegroundColor Green
            } else {
                Write-Host "GPU acceleration already enabled in Docker Desktop" -ForegroundColor Green
            }
            
            # Check and enable WSL 2 engine
            Write-Host "Checking WSL 2 engine setting..." -ForegroundColor Yellow
            if (-not $settings.useWsl2Engine) {
                $settings.useWsl2Engine = $true
                $modified = $true
                Write-Host "WSL 2 engine enabled in Docker Desktop" -ForegroundColor Green
            } else {
                Write-Host "WSL 2 engine already enabled in Docker Desktop" -ForegroundColor Green
            }
            
            # Save changes if modified
            if ($modified) {
                Write-Host ""
                Write-Host "Saving Docker Desktop configuration..." -ForegroundColor Yellow
                $settings | ConvertTo-Json -Depth 10 | Set-Content $dockerSettingsPath
                Write-Host "Docker Desktop configuration updated" -ForegroundColor Green
                Write-Host ""
                Write-Host "IMPORTANT: Docker Desktop restart required" -ForegroundColor Yellow
                Write-Host "   Please restart Docker Desktop to apply GPU settings" -ForegroundColor Yellow
                Write-Host "   You can do this from the Docker Desktop system tray icon" -ForegroundColor Gray
            } else {
                Write-Host ""
                Write-Host "Docker Desktop already configured for GPU support" -ForegroundColor Green
            }
            
            return $true
        } else {
            Write-Host "Docker Desktop settings file not found" -ForegroundColor Red
            Write-Host "   This usually means Docker Desktop is not installed or not running" -ForegroundColor Gray
            Write-Host ""
            Write-Host "Manual configuration required:" -ForegroundColor Yellow
            Write-Host "1. Open Docker Desktop" -ForegroundColor Gray
            Write-Host "2. Go to Settings > General" -ForegroundColor Gray
            Write-Host "3. Enable 'Use the WSL 2 based engine'" -ForegroundColor Gray
            Write-Host "4. Go to Settings > Resources > WSL Integration" -ForegroundColor Gray
            Write-Host "5. Enable GPU acceleration" -ForegroundColor Gray
            return $false
        }
    } catch {
        Write-Host ""
        Write-Host "ERROR: Failed to configure Docker Desktop" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Manual configuration required:" -ForegroundColor Yellow
        Write-Host "1. Open Docker Desktop Settings" -ForegroundColor Gray
        Write-Host "2. Enable WSL 2 engine and GPU acceleration" -ForegroundColor Gray
        Write-Host "3. Restart Docker Desktop" -ForegroundColor Gray
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
        Write-Host ""
        Write-Host "Auto-install mode enabled - proceeding with installation" -ForegroundColor Green
        return $true
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Installation Required" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Component: $Component" -ForegroundColor Cyan
    Write-Host "Purpose: $Description" -ForegroundColor White
    Write-Host ""
    Write-Host "This component is required for GPU acceleration." -ForegroundColor Gray
    Write-Host "Without it, Ollama will run on CPU (slower performance)." -ForegroundColor Gray
    Write-Host ""
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
            $content = "version: '3.8'

services:
  ollama:
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]"
        } else {
            Write-Host "Creating GPU configuration..." -ForegroundColor Yellow
            $content = "version: '3.8'

services:
  ollama:
    deploy:
      resources:
        reservations:
          devices:
            - driver: nvidia
              count: all
              capabilities: [gpu]"
        }
    } else {
        Write-Host "Creating CPU-only configuration..." -ForegroundColor Yellow
        $content = "version: '3.8'

services:
  ollama:
    # No GPU configuration"
    }
    $content | Out-File -FilePath "docker-compose.gpu.yml" -Encoding UTF8
    Write-Host "Configuration file created" -ForegroundColor Green
}

# Function to start services
function Start-Services {
    param(
        [bool]$GPUAvailable
    )
    
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

# Function to check service status
function Test-ServiceStatus {
    Write-Host "Service Status" -ForegroundColor Cyan
    $services = @(
        @{Name="Qdrant"; URL="http://localhost:6333/collections"},
        @{Name="Ollama"; URL="http://localhost:11434/api/tags"},
        @{Name="LangChain service"; URL="http://localhost:8000/health"}
    )
    foreach ($service in $services) {
        Write-Host ("Checking {0}..." -f $service.Name) -ForegroundColor Yellow
        try {
            $response = Invoke-WebRequest -Uri $service.URL -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host ("{0} is running" -f $service.Name) -ForegroundColor Green
            } else {
                Write-Host ("{0} is not responding" -f $service.Name) -ForegroundColor Red
            }
        } catch {
            Write-Host ("{0} is not responding" -f $service.Name) -ForegroundColor Red
        }
    }
}

# Function to show deployment summary
function Show-DeploymentSummary {
    param(
        [bool]$GPUAvailable,
        [bool]$ContainerGPU
    )
    
    Write-Host "Deployment Summary" -ForegroundColor Cyan
    if ($ForceCPU) {
        Write-Host "CPU-only deployment (forced)" -ForegroundColor Yellow
        Write-Host "  - GPU support disabled by -ForceCPU parameter" -ForegroundColor Gray
        Write-Host "  - Ollama running on CPU" -ForegroundColor Gray
    } elseif ($GPUAvailable) {
        if ($ContainerGPU) {
            Write-Host "GPU-accelerated deployment successful" -ForegroundColor Green
            Write-Host "  - NVIDIA GPU detected and enabled" -ForegroundColor Gray
            Write-Host "  - NVIDIA Container Toolkit available" -ForegroundColor Gray
            Write-Host "  - Ollama running with GPU acceleration" -ForegroundColor Gray
        } else {
            Write-Host "GPU detected but Container Toolkit not available" -ForegroundColor Yellow
            Write-Host "  - NVIDIA GPU detected" -ForegroundColor Gray
            Write-Host "  - Install NVIDIA Container Toolkit for GPU acceleration" -ForegroundColor Gray
            Write-Host "  - Currently running on CPU" -ForegroundColor Gray
        }
    } else {
        Write-Host "CPU-only deployment successful" -ForegroundColor Yellow
        Write-Host "  - No NVIDIA GPU detected" -ForegroundColor Gray
        Write-Host "  - Ollama running on CPU" -ForegroundColor Gray
        Write-Host "  - Performance may be slower than GPU version" -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "Services are ready:" -ForegroundColor Cyan
    Write-Host "- Qdrant Vector Database: http://localhost:6333" -ForegroundColor White
    Write-Host "- Ollama LLM Service: http://localhost:11434" -ForegroundColor White
    Write-Host "- LangChain Search Service: http://localhost:8000" -ForegroundColor White
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
    Write-Host ("ERROR: {0}" -f $_.Exception.Message) -ForegroundColor Red
    if ($Verbose) {
        Write-Host $_.Exception.StackTrace -ForegroundColor Red
    }
    exit 1
}

Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 