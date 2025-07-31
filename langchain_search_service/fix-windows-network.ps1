# Windows Network Fix Script for CHAP2 Deployment
Write-Host "========================================" -ForegroundColor Green
Write-Host "Windows Network Configuration Fix" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Get server IP address
Write-Host "Getting server IP address..." -ForegroundColor Yellow
$ipAddress = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object {$_.IPAddress -notlike "169.254.*" -and $_.IPAddress -notlike "127.*"} | Select-Object -First 1).IPAddress
Write-Host "Server IP: $ipAddress" -ForegroundColor Green
Write-Host ""

# Test local connections with different methods
Write-Host "Testing local connections..." -ForegroundColor Yellow

# Test Web Portal
Write-Host "Testing Web Portal (port 5000):" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  Localhost: OK" -ForegroundColor Green
} catch {
    Write-Host "  Localhost: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:5000" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  127.0.0.1: OK" -ForegroundColor Green
} catch {
    Write-Host "  127.0.0.1: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://$ipAddress`:5000" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  $ipAddress`:5000: OK" -ForegroundColor Green
} catch {
    Write-Host "  $ipAddress`:5000: FAILED" -ForegroundColor Red
}

Write-Host ""

# Test API
Write-Host "Testing API (port 5001):" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5001" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  Localhost: OK" -ForegroundColor Green
} catch {
    Write-Host "  Localhost: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:5001" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  127.0.0.1: OK" -ForegroundColor Green
} catch {
    Write-Host "  127.0.0.1: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://$ipAddress`:5001" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  $ipAddress`:5001: OK" -ForegroundColor Green
} catch {
    Write-Host "  $ipAddress`:5001: FAILED" -ForegroundColor Red
}

Write-Host ""

# Test LangChain Service
Write-Host "Testing LangChain Service (port 8000):" -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:8000/health" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  Localhost: OK" -ForegroundColor Green
} catch {
    Write-Host "  Localhost: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:8000/health" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  127.0.0.1: OK" -ForegroundColor Green
} catch {
    Write-Host "  127.0.0.1: FAILED" -ForegroundColor Red
}

try {
    $response = Invoke-WebRequest -Uri "http://$ipAddress`:8000/health" -TimeoutSec 5 -UseBasicParsing
    Write-Host "  $ipAddress`:8000: OK" -ForegroundColor Green
} catch {
    Write-Host "  $ipAddress`:8000: FAILED" -ForegroundColor Red
}

Write-Host ""

# Check Windows Firewall
Write-Host "Checking Windows Firewall rules..." -ForegroundColor Yellow
$firewallRules = Get-NetFirewallRule | Where-Object {$_.DisplayName -like "*Docker*" -or $_.DisplayName -like "*5000*" -or $_.DisplayName -like "*5001*" -or $_.DisplayName -like "*8000*"}
if ($firewallRules) {
    Write-Host "Found existing firewall rules:" -ForegroundColor Green
    $firewallRules | ForEach-Object { Write-Host "  $($_.DisplayName) - $($_.Enabled)" -ForegroundColor Cyan }
} else {
    Write-Host "No specific firewall rules found for these ports" -ForegroundColor Yellow
}

Write-Host ""

# Provide access URLs
Write-Host "Access URLs:" -ForegroundColor Green
Write-Host "  Web Portal: http://$ipAddress`:5000" -ForegroundColor Cyan
Write-Host "  API: http://$ipAddress`:5001" -ForegroundColor Cyan
Write-Host "  LangChain: http://$ipAddress`:8000" -ForegroundColor Cyan
Write-Host ""

Write-Host "If you still can't access from another machine:" -ForegroundColor Yellow
Write-Host "1. Check Windows Firewall settings" -ForegroundColor White
Write-Host "2. Ensure ports 5000, 5001, 8000, 6333 are open" -ForegroundColor White
Write-Host "3. Check if server is behind a corporate firewall" -ForegroundColor White
Write-Host "4. Try using the server's IP address instead of hostname" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 