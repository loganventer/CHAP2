#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test for duplicate results in AI search
.DESCRIPTION
    Tests AI search to identify if duplicates are being returned
#>

Write-Host "=== Testing for Duplicate Results ===" -ForegroundColor Cyan
Write-Host ""

# Test AI search and check for duplicates
Write-Host "Testing AI search for 'Jesus'..." -ForegroundColor Yellow
try {
    $body = @{
        query = "Jesus"
        maxResults = 10
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/IntelligentSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 30
    
    Write-Host "   Total results: $($response.searchResults.Count)" -ForegroundColor Green
    
    # Check for duplicates
    $names = $response.searchResults | ForEach-Object { $_.name }
    $uniqueNames = $names | Sort-Object | Get-Unique
    $duplicateCount = $names.Count - $uniqueNames.Count
    
    Write-Host "   Unique names: $($uniqueNames.Count)" -ForegroundColor Green
    Write-Host "   Duplicate count: $duplicateCount" -ForegroundColor $(if ($duplicateCount -gt 0) { "Red" } else { "Green" })
    
    if ($duplicateCount -gt 0) {
        Write-Host "   Duplicate names found:" -ForegroundColor Red
        $duplicates = $names | Group-Object | Where-Object { $_.Count -gt 1 }
        foreach ($dup in $duplicates) {
            Write-Host "     - '$($dup.Name)' appears $($dup.Count) times" -ForegroundColor Red
        }
    } else {
        Write-Host "   No duplicates found" -ForegroundColor Green
    }
    
    Write-Host "   First 5 results:" -ForegroundColor Green
    for ($i = 0; $i -lt [Math]::Min(5, $response.searchResults.Count); $i++) {
        $result = $response.searchResults[$i]
        Write-Host "     $($i + 1). $($result.name) (Score: $($result.score))" -ForegroundColor White
    }
    
} catch {
    Write-Host "   ❌ FAILED: AI search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test RAG search
Write-Host "Testing RAG search for 'love'..." -ForegroundColor Yellow
try {
    $body = @{
        query = "What are some songs about love?"
        maxResults = 5
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/RagSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 45
    
    Write-Host "   RAG response length: $($response.response.Length) characters" -ForegroundColor Green
    Write-Host "   First 200 chars: $($response.response.Substring(0, [Math]::Min(200, $response.response.Length)))..." -ForegroundColor Green
    
} catch {
    Write-Host "   ❌ FAILED: RAG search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test traditional search for comparison
Write-Host "Testing traditional search for 'Jesus'..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/Search?q=Jesus" -Method Get -TimeoutSec 10
    
    Write-Host "   Traditional search results: $($response.results.Count)" -ForegroundColor Green
    
    # Check for duplicates in traditional search
    $names = $response.results | ForEach-Object { $_.name }
    $uniqueNames = $names | Sort-Object | Get-Unique
    $duplicateCount = $names.Count - $uniqueNames.Count
    
    Write-Host "   Traditional search duplicates: $duplicateCount" -ForegroundColor $(if ($duplicateCount -gt 0) { "Red" } else { "Green" })
    
} catch {
    Write-Host "   ❌ FAILED: Traditional search failed - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "If duplicates are found in AI search but not traditional search:" -ForegroundColor Yellow
Write-Host "  - Issue is in AI search processing" -ForegroundColor White
Write-Host "  - Check LangChain service configuration" -ForegroundColor White
Write-Host "  - Check vector search implementation" -ForegroundColor White
Write-Host ""
Write-Host "If duplicates are found in both:" -ForegroundColor Yellow
Write-Host "  - Issue is in data or API service" -ForegroundColor White
Write-Host "  - Check chorus data for duplicates" -ForegroundColor White
Write-Host "  - Check API service logic" -ForegroundColor White 