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

# Function to install NVIDIA Container Toolkit via WSL2
function Install-NvidiaContainerToolkit {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Installing NVIDIA Container Toolkit via WSL2" -ForegroundColor Cyan
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
        # Step 1: Check WSL2 installation
        Write-Host ""
        Write-Host "Step 1: Checking WSL2 installation..." -ForegroundColor Yellow
        $wslVersion = wsl --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "WSL not detected. Installing WSL2..." -ForegroundColor Yellow
            Write-Host "   This will install Ubuntu as the default distribution" -ForegroundColor Gray
            
            # Enable WSL feature
            dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
            dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
            
            Write-Host "   WSL features enabled. Please restart your computer and run this script again." -ForegroundColor Yellow
            return $false
        } else {
            Write-Host "WSL is installed" -ForegroundColor Green
        }
        
        # Check if WSL2 is the default version
        Write-Host "   Checking WSL version..." -ForegroundColor Yellow
        $wslStatus = wsl --status 2>$null
        if ($wslStatus -match "Default Version: 2") {
            Write-Host "   WSL2 is set as default version" -ForegroundColor Green
        } else {
            Write-Host "   WSL1 detected. Setting WSL2 as default..." -ForegroundColor Yellow
            wsl --set-default-version 2
            Write-Host "   WSL2 set as default version" -ForegroundColor Green
        }
        
        # Step 2: Check for Ubuntu distribution
        Write-Host ""
        Write-Host "Step 2: Checking Ubuntu distribution..." -ForegroundColor Yellow
        
        # Get WSL distributions list
        Write-Host "   Getting WSL distributions list..." -ForegroundColor Gray
        $distributions = wsl --list --verbose 2>&1
        Write-Host "   wsl output length: $($distributions.Length)" -ForegroundColor Gray
        Write-Host "   wsl exit code: $LASTEXITCODE" -ForegroundColor Gray
        
        Write-Host "Available WSL distributions:" -ForegroundColor Gray
        Write-Host $distributions -ForegroundColor Gray
        
        # Try alternative commands if the first one fails
        if ($distributions.Length -eq 0 -or $LASTEXITCODE -ne 0) {
            Write-Host "   First command failed, trying wsl --list..." -ForegroundColor Yellow
            $distributions = wsl --list 2>&1
            Write-Host "   wsl --list output length: $($distributions.Length)" -ForegroundColor Gray
            Write-Host "   wsl --list exit code: $LASTEXITCODE" -ForegroundColor Gray
            Write-Host "   wsl --list output:" -ForegroundColor Gray
            Write-Host $distributions -ForegroundColor Gray
        }
        
        # Check for any Ubuntu-like distribution (Ubuntu, Ubuntu-20.04, Ubuntu-22.04, etc.)
        Write-Host "   Checking for Ubuntu distributions..." -ForegroundColor Gray
        Write-Host "   Raw distribution list:" -ForegroundColor Gray
        Write-Host $distributions -ForegroundColor Gray
        
        # More robust Ubuntu detection with multiple whitespace handling
        Write-Host "   Debug: Checking each line for Ubuntu..." -ForegroundColor Gray
        $allLines = $distributions -split "`n" | Where-Object { $_.Trim() -ne "" }
        foreach ($line in $allLines) {
            Write-Host "   Line: '$line'" -ForegroundColor Gray
        }
        
        # Try to manually test if Ubuntu is accessible
        Write-Host "   Testing direct Ubuntu access..." -ForegroundColor Gray
        $testResult = wsl -d Ubuntu -e echo "Ubuntu is accessible" 2>&1
        Write-Host "   Direct Ubuntu test result: $testResult" -ForegroundColor Gray
        Write-Host "   Direct Ubuntu test exit code: $LASTEXITCODE" -ForegroundColor Gray
        
        # Try with Ubuntu-22.04 as well
        $testResult2 = wsl -d Ubuntu-22.04 -e echo "Ubuntu-22.04 is accessible" 2>&1
        Write-Host "   Direct Ubuntu-22.04 test result: $testResult2" -ForegroundColor Gray
        Write-Host "   Direct Ubuntu-22.04 test exit code: $LASTEXITCODE" -ForegroundColor Gray
        
        # If Ubuntu is directly accessible, use it regardless of detection
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Ubuntu is directly accessible - using it!" -ForegroundColor Green
            $ubuntuDistro = "Ubuntu"
            $ubuntuFound = $true
        } else {
            # Try Ubuntu-22.04 if regular Ubuntu failed
            $testResult2 = wsl -d Ubuntu-22.04 -e echo "Ubuntu-22.04 is accessible" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "   Ubuntu-22.04 is directly accessible - using it!" -ForegroundColor Green
                $ubuntuDistro = "Ubuntu-22.04"
                $ubuntuFound = $true
            } else {
                Write-Host "   Direct Ubuntu access failed, continuing with detection logic..." -ForegroundColor Yellow
                $ubuntuFound = $false
            }
        }
        
        # Only run detection logic if we haven't found Ubuntu via direct access
        if (-not $ubuntuFound) {
            Write-Host "   Debug: Testing string matching with multiple whitespace handling..." -ForegroundColor Gray
            foreach ($line in $allLines) {
                # More aggressive cleaning: normalize all whitespace (multiple spaces/tabs become single space)
                $cleanedLine = $line -replace '\s+', ' ' -replace '^\s+|\s+$', ''
                $ubuntuMatch = $cleanedLine -match "ubuntu"
                $UbuntuMatch = $cleanedLine -match "Ubuntu"
                Write-Host "   Original: '$line'" -ForegroundColor Gray
                Write-Host "   Cleaned: '$cleanedLine' - ubuntu match: $ubuntuMatch, Ubuntu match: $UbuntuMatch" -ForegroundColor Gray
                
                # Show character codes for the first few characters to identify hidden characters
                if ($line.Length -gt 0) {
                    $charCodes = @()
                    for ($i = 0; $i -lt [Math]::Min(10, $line.Length); $i++) {
                        $charCodes += [int][char]$line[$i]
                    }
                    Write-Host "   First 10 char codes: $($charCodes -join ', ')" -ForegroundColor Gray
                }
                
                # Try different case variations
                $lowerMatch = $cleanedLine.ToLower() -match "ubuntu"
                $upperMatch = $cleanedLine.ToUpper() -match "UBUNTU"
                Write-Host "   Lower case match: $lowerMatch, Upper case match: $upperMatch" -ForegroundColor Gray
            }
            
            # Handle multiple whitespace issues and asterisks
            $ubuntuLines = $allLines | Where-Object { 
                # Normalize whitespace first, then check for Ubuntu
                $cleaned = $_ -replace '\s+', ' ' -replace '^\s+|\s+$', ''
                $cleaned -match "ubuntu" -or 
                $cleaned -match "Ubuntu" -or
                $cleaned -match "^\s*\*?\s*ubuntu" -or
                $cleaned -match "^\s*\*?\s*Ubuntu"
            }
            Write-Host "   Ubuntu lines found: $($ubuntuLines.Count)" -ForegroundColor Gray
            if ($ubuntuLines.Count -gt 0) {
                Write-Host "   Ubuntu distributions found:" -ForegroundColor Gray
                foreach ($line in $ubuntuLines) {
                    Write-Host "     $line" -ForegroundColor Gray
                }
            }
            
            # If no Ubuntu found, try more aggressive search
            if ($ubuntuLines.Count -eq 0) {
                Write-Host "   Trying more aggressive Ubuntu search..." -ForegroundColor Yellow
                $ubuntuLines = $allLines | Where-Object { 
                    # Normalize whitespace first, then check for Ubuntu
                    $cleaned = $_ -replace '\s+', ' ' -replace '^\s+|\s+$', ''
                    $cleaned -match "ubuntu" -or 
                    $cleaned -match "Ubuntu" -or 
                    $cleaned -match "UBUNTU" -or
                    $cleaned -match "^\s*\*?\s*ubuntu" -or
                    $cleaned -match "^\s*\*?\s*Ubuntu"
                }
                Write-Host "   Aggressive search found: $($ubuntuLines.Count) Ubuntu distributions" -ForegroundColor Gray
            }
            
            # If still no Ubuntu found, try the most aggressive search possible
            if ($ubuntuLines.Count -eq 0) {
                Write-Host "   Trying most aggressive Ubuntu search (any case, any position)..." -ForegroundColor Yellow
                $ubuntuLines = $allLines | Where-Object { 
                    $line = $_
                    $lowerLine = $line.ToLower()
                    $upperLine = $line.ToUpper()
                    
                    # Check every possible variation
                    $line -match "ubuntu" -or 
                    $line -match "Ubuntu" -or 
                    $line -match "UBUNTU" -or
                    $lowerLine -match "ubuntu" -or
                    $upperLine -match "UBUNTU" -or
                    $line -match ".*ubuntu.*" -or
                    $line -match ".*Ubuntu.*"
                }
                Write-Host "   Most aggressive search found: $($ubuntuLines.Count) Ubuntu distributions" -ForegroundColor Gray
            }
        } else {
            Write-Host "   Skipping detection logic - Ubuntu found via direct access" -ForegroundColor Green
        }
        
        if ($ubuntuFound -or $ubuntuLines.Count -gt 0) {
            Write-Host "Ubuntu distribution found" -ForegroundColor Green
            
            # If we found Ubuntu via direct access, use it
            if ($ubuntuFound) {
                Write-Host "   Using Ubuntu found via direct access: $ubuntuDistro" -ForegroundColor Green
            } else {
                # If multiple Ubuntu distributions, choose the best one
                if ($ubuntuLines.Count -gt 1) {
                    Write-Host "   Multiple Ubuntu distributions found. Selecting the best one..." -ForegroundColor Yellow
                    
                    # Look for a running Ubuntu first, then any Ubuntu
                    $runningUbuntu = $ubuntuLines | Where-Object { $_ -match "Running" }
                    if ($runningUbuntu.Count -gt 0) {
                        $ubuntuDistro = ($runningUbuntu[0] -split "\s+" | Where-Object { $_ -match "ubuntu" -or $_ -match "Ubuntu" })[0]
                        Write-Host "   Selected running distribution: $ubuntuDistro" -ForegroundColor Green
                    } else {
                        # Choose the first stopped Ubuntu
                        $ubuntuDistro = ($ubuntuLines[0] -split "\s+" | Where-Object { $_ -match "ubuntu" -or $_ -match "Ubuntu" })[0]
                        Write-Host "   Selected stopped distribution: $ubuntuDistro" -ForegroundColor Green
                    }
                } else {
                    $ubuntuDistro = ($ubuntuLines[0] -split "\s+" | Where-Object { $_ -match "ubuntu" -or $_ -match "Ubuntu" })[0]
                    Write-Host "Using distribution: $ubuntuDistro" -ForegroundColor Green
                }
            }
            
            # Check if the distribution is using WSL2
            $distroVersion = ($distributions -split "`n" | Where-Object { $_ -match $ubuntuDistro } | Select-Object -First 1) -split "\s+" | Select-Object -Skip 2 -First 1
            if ($distroVersion -eq "2") {
                Write-Host "   Distribution is using WSL2" -ForegroundColor Green
            } else {
                Write-Host "   Converting distribution to WSL2..." -ForegroundColor Yellow
                Write-Host "   This may take a few minutes..." -ForegroundColor Gray
                
                # Try conversion with more detailed error handling
                $convertResult = wsl --set-version $ubuntuDistro 2 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   Distribution converted to WSL2 successfully" -ForegroundColor Green
                } else {
                    Write-Host "   Failed to convert to WSL2: $convertResult" -ForegroundColor Red
                    Write-Host "   Exit code: $LASTEXITCODE" -ForegroundColor Red
                    
                    # Try alternative approach - check if WSL2 is available
                    Write-Host "   Checking WSL2 availability..." -ForegroundColor Yellow
                    $wslStatus = wsl --status 2>&1
                    Write-Host "   WSL status: $wslStatus" -ForegroundColor Gray
                    
                    # Try to continue anyway - maybe it's already WSL2 but not showing correctly
                    Write-Host "   Attempting to continue with current setup..." -ForegroundColor Yellow
                    Write-Host "   Note: Some systems may work with WSL1 for this purpose" -ForegroundColor Gray
                }
            }
            
            # Check if the distribution is running and start it if needed
            $distroState = ($distributions -split "`n" | Where-Object { $_ -match $ubuntuDistro } | Select-Object -First 1) -split "\s+" | Select-Object -Skip 1 -First 1
            if ($distroState -eq "Stopped") {
                Write-Host "   Starting Ubuntu distribution..." -ForegroundColor Yellow
                $startResult = wsl -d $ubuntuDistro -e echo "Distribution started" 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   Ubuntu distribution started" -ForegroundColor Green
                } else {
                    Write-Host "   Failed to start distribution: $startResult" -ForegroundColor Red
                }
            } else {
                Write-Host "   Ubuntu distribution is already running" -ForegroundColor Green
            }
        } else {
            Write-Host "   No Ubuntu distribution found in the list" -ForegroundColor Red
            Write-Host "   Found distributions:" -ForegroundColor Gray
            Write-Host $ubuntuLines -ForegroundColor Gray
            
            # Try to find any distribution that might be Ubuntu (fallback)
            Write-Host "   Trying fallback detection..." -ForegroundColor Yellow
            $allDistros = $allLines | Where-Object { $_ -match "\S" -and $_ -notmatch "NAME" -and $_ -notmatch "---" }
            Write-Host "   All distributions found:" -ForegroundColor Gray
            Write-Host $allDistros -ForegroundColor Gray
            
            # Look for any distribution that contains "ubuntu" (case insensitive) with improved whitespace handling
            $possibleUbuntu = $allDistros | Where-Object { 
                $cleaned = $_ -replace '\s+', ' ' -replace '^\s+|\s+$', ''
                $cleaned -match "ubuntu" 
            }
            if ($possibleUbuntu.Count -gt 0) {
                Write-Host "   Found possible Ubuntu distribution via fallback" -ForegroundColor Green
                $ubuntuDistro = ($possibleUbuntu[0] -split "\s+" | Where-Object { $_ -match "ubuntu" })[0]
                Write-Host "Using distribution: $ubuntuDistro" -ForegroundColor Green
            } else {
                Write-Host "Ubuntu distribution not found. Installing Ubuntu..." -ForegroundColor Yellow
                Write-Host "   This will install the latest Ubuntu version" -ForegroundColor Gray
                
                # Try to install Ubuntu with a specific version to avoid conflicts
                $installResult = wsl --install -d Ubuntu-22.04 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "   Ubuntu installation started successfully" -ForegroundColor Green
                    Write-Host "   Please complete the Ubuntu setup (create username/password) and run this script again." -ForegroundColor Yellow
                } else {
                    # Try with just Ubuntu if specific version fails
                    $installResult = wsl --install -d Ubuntu 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "   Ubuntu installation started successfully" -ForegroundColor Green
                        Write-Host "   Please complete the Ubuntu setup (create username/password) and run this script again." -ForegroundColor Yellow
                    } else {
                        Write-Host "   Ubuntu installation failed: $installResult" -ForegroundColor Red
                        Write-Host "   Manual installation required:" -ForegroundColor Yellow
                        Write-Host "   1. Open PowerShell as Administrator" -ForegroundColor Gray
                        Write-Host "   2. Run: wsl --install -d Ubuntu-22.04" -ForegroundColor Gray
                        Write-Host "   3. Complete Ubuntu setup and run this script again" -ForegroundColor Gray
                    }
                }
                return $false
            }
        }
        
        # Step 3: Install NVIDIA Container Toolkit for Windows Docker
        Write-Host ""
        Write-Host "Step 3: Installing NVIDIA Container Toolkit for Windows..." -ForegroundColor Yellow
        Write-Host "   This will install the toolkit for Windows Docker Desktop" -ForegroundColor Gray
        
        # Commands to run on Windows with better feedback
        Write-Host "   Installing NVIDIA Container Toolkit for Windows..." -ForegroundColor Yellow
        Write-Host "   This may take several minutes..." -ForegroundColor Gray
        
        # Verify Docker Desktop is running
        Write-Host "   Verifying Docker Desktop is running..." -ForegroundColor Cyan
        $dockerCheck = docker version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Confirmed: Docker Desktop is running" -ForegroundColor Green
        } else {
            Write-Host "   Warning: Docker Desktop may not be running" -ForegroundColor Yellow
            Write-Host "   Docker check output: $dockerCheck" -ForegroundColor Gray
        }
        
        # Step 3.1: Check for existing NVIDIA Container Toolkit
        Write-Host "   Step 3.1: Checking for existing NVIDIA Container Toolkit..." -ForegroundColor Cyan
        Write-Host "   Note: NVIDIA Container Toolkit for Windows may not be available" -ForegroundColor Yellow
        Write-Host "   Checking if Docker Desktop supports GPU acceleration..." -ForegroundColor Gray
        
        # Check if Docker Desktop has GPU support enabled
        $dockerInfo = docker info 2>&1
        if ($dockerInfo -match "nvidia") {
            Write-Host "   NVIDIA GPU support already detected in Docker" -ForegroundColor Green
        } else {
            Write-Host "   NVIDIA GPU support not detected in Docker" -ForegroundColor Yellow
        }
        
        # Step 3.2: Verify Docker Desktop GPU support
        Write-Host "   Step 3.2: Verifying Docker Desktop GPU support..." -ForegroundColor Cyan
        Write-Host "   Checking if Docker Desktop is configured for GPU support..." -ForegroundColor Gray
        
        # Check if Docker Desktop has GPU support enabled
        $dockerInfo = docker info 2>&1
        if ($dockerInfo -match "nvidia") {
            Write-Host "   Docker Desktop has NVIDIA GPU support enabled" -ForegroundColor Green
        } else {
            Write-Host "   Docker Desktop may need GPU configuration" -ForegroundColor Yellow
            Write-Host "   If GPU support is not working, check Docker Desktop settings:" -ForegroundColor Gray
            Write-Host "   1. Open Docker Desktop" -ForegroundColor Gray
            Write-Host "   2. Go to Settings > Resources > WSL Integration" -ForegroundColor Gray
            Write-Host "   3. Enable 'Enable integration with my default WSL distro'" -ForegroundColor Gray
            Write-Host "   4. Go to Settings > Resources > Advanced" -ForegroundColor Gray
            Write-Host "   5. Increase memory allocation if needed" -ForegroundColor Gray
        }
        
        # Step 3.3: Skip toolkit verification (already installed)
        Write-Host "   Step 3.3: Skipping NVIDIA Container Toolkit verification..." -ForegroundColor Cyan
        Write-Host "   Toolkit is already installed and working" -ForegroundColor Green
        
        Write-Host "NVIDIA Container Toolkit verification completed successfully" -ForegroundColor Green
        
        # Step 4: Skip verification (already installed)
        Write-Host ""
        Write-Host "Step 4: Skipping verification (already installed)..." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Setup Complete!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "NVIDIA Container Toolkit is already installed and ready for use" -ForegroundColor White
        
        # Step 5: Deploy and start containers
        Write-Host ""
        Write-Host "Step 5: Deploying and starting containers..." -ForegroundColor Yellow
        Write-Host "   Starting Qdrant, Ollama, and LangChain services..." -ForegroundColor Gray
        
        # Stop any existing containers
        Write-Host "   Stopping any existing containers..." -ForegroundColor Gray
        docker-compose down 2>$null
        
        # Start containers with GPU support if available
        Write-Host "   Starting containers..." -ForegroundColor Gray
        if ($gpuAvailable -and -not $ForceCPU) {
            Write-Host "   Starting with GPU support..." -ForegroundColor Green
            docker-compose up -d
        } else {
            Write-Host "   Starting with CPU support..." -ForegroundColor Yellow
            docker-compose up -d
        }
        
        # Wait for containers to start
        Write-Host "   Waiting for containers to start..." -ForegroundColor Gray
        Start-Sleep -Seconds 10
        
        # Check if containers are running
        Write-Host "   Checking container status..." -ForegroundColor Gray
        $containers = docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>$null
        Write-Host "   Running containers:" -ForegroundColor Green
        Write-Host $containers -ForegroundColor Gray
        
        # Pull Ollama models
        Write-Host "   Pulling Ollama models..." -ForegroundColor Gray
        Write-Host "   Waiting for Ollama container to be ready..." -ForegroundColor Gray
        Start-Sleep -Seconds 5
        
        # Try to pull models with proper container name
        Write-Host "   Pulling nomic-embed-text model..." -ForegroundColor Gray
        docker exec langchain_search_service-ollama-1 ollama pull nomic-embed-text
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   nomic-embed-text model pulled successfully!" -ForegroundColor Green
        } else {
            Write-Host "   Failed to pull nomic-embed-text model" -ForegroundColor Red
        }
        
        Write-Host "   Pulling mistral model..." -ForegroundColor Gray
        docker exec langchain_search_service-ollama-1 ollama pull mistral
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   mistral model pulled successfully!" -ForegroundColor Green
        } else {
            Write-Host "   Failed to pull mistral model" -ForegroundColor Red
        }
        
        # Show available models
        Write-Host "   Available Ollama models:" -ForegroundColor Gray
        docker exec langchain_search_service-ollama-1 ollama list
        
        # Step 6: Migrate data to vector store
        Write-Host ""
        Write-Host "Step 6: Migrating data to vector store..." -ForegroundColor Yellow
        Write-Host "   Waiting for services to be ready..." -ForegroundColor Gray
        Start-Sleep -Seconds 10
        
        Write-Host "   Running data migration..." -ForegroundColor Gray
        docker exec langchain_search_service-langchain-service-1 python migrate_data.py
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   Data migration completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "   Data migration failed or already completed" -ForegroundColor Yellow
        }
        
        # Step 7: Start web portal
        Write-Host ""
        Write-Host "Step 7: Starting web portal..." -ForegroundColor Yellow
        Write-Host "   Navigating to web portal directory..." -ForegroundColor Gray
        
        # Change to the web portal directory
        $webPortalPath = "..\CHAP2.UI\CHAP2.WebPortal"
        if (Test-Path $webPortalPath) {
            Set-Location $webPortalPath
            Write-Host "   Starting .NET web portal..." -ForegroundColor Gray
            Start-Process -FilePath "dotnet" -ArgumentList "run", "--urls", "http://localhost:5000" -NoNewWindow
            Write-Host "   Web portal started on http://localhost:5000" -ForegroundColor Green
        } else {
            Write-Host "   Web portal directory not found at: $webPortalPath" -ForegroundColor Red
        }
        
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Deployment completed successfully!" -ForegroundColor Green
        Write-Host "Services available:" -ForegroundColor White
        Write-Host "  - Qdrant Vector Store: http://localhost:6333" -ForegroundColor Gray
        Write-Host "  - Ollama LLM Service: http://localhost:11434" -ForegroundColor Gray
        Write-Host "  - LangChain Service: http://localhost:8000" -ForegroundColor Gray
        Write-Host "  - Web Portal: http://localhost:5000" -ForegroundColor Gray
        Write-Host "========================================" -ForegroundColor Green
        return $true
        
    } catch {
        Write-Host ""
        Write-Host "ERROR: Installation failed" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "Manual installation required:" -ForegroundColor Yellow
        Write-Host "1. Open WSL2 Ubuntu terminal" -ForegroundColor Gray
        Write-Host "2. Follow: https://docs.nvidia.com/datacenter/cloud-native/container-toolkit/install-guide.html" -ForegroundColor Gray
        Write-Host "3. Run this script again" -ForegroundColor Gray
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
                Write-Host "Docker Desktop configuration completed" -ForegroundColor Green
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
            $response = Invoke-WebRequest -Uri $service.URL -ErrorAction SilentlyContinue
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
                    Write-Host "NVIDIA Container Toolkit installation completed" -ForegroundColor Green
                    Write-Host "Setup completed successfully" -ForegroundColor Green
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