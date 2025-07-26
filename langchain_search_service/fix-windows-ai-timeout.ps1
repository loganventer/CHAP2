#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test AI search with proper timeouts for Windows
.DESCRIPTION
    Tests AI search endpoints with longer timeouts to handle processing delays
#>

Write-Host "=== Testing AI Search with Proper Timeouts ===" -ForegroundColor Cyan
Write-Host ""

# Test AI search with longer timeout
Write-Host "Testing AI search with 30-second timeout..." -ForegroundColor Yellow
try {
    $body = @{
        query = "Jesus"
        maxResults = 3
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5000/Home/IntelligentSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 30
    
    if ($response.searchResults -and $response.searchResults.Count -gt 0) {
        Write-Host "   ✅ SUCCESS: AI search returned $($response.searchResults.Count) results" -ForegroundColor Green
        Write-Host "   First result: $($response.searchResults[0].name)" -ForegroundColor Green
        Write-Host "   AI Analysis: $($response.aiAnalysis)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  WARNING: No results returned" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ FAILED: AI search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test RAG search with longer timeout
Write-Host "Testing RAG search with 45-second timeout..." -ForegroundColor Yellow
try {
    $body = @{
        query = "What are some songs about love?"
        maxResults = 3
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5000/Home/RagSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 45
    
    if ($response.response -and $response.response.Length -gt 0) {
        Write-Host "   ✅ SUCCESS: RAG search returned response" -ForegroundColor Green
        Write-Host "   Response length: $($response.response.Length) characters" -ForegroundColor Green
        Write-Host "   First 100 chars: $($response.response.Substring(0, [Math]::Min(100, $response.response.Length)))..." -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  WARNING: No RAG response" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ FAILED: RAG search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test traditional search (should be fast)
Write-Host "Testing traditional search..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5000/Home/Search?q=Jesus" -Method Get -TimeoutSec 10
    
    if ($response.results -and $response.results.Count -gt 0) {
        Write-Host "   ✅ SUCCESS: Traditional search returned $($response.results.Count) results" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  WARNING: No traditional search results" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ FAILED: Traditional search failed - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "If AI search works with longer timeout:" -ForegroundColor Yellow
Write-Host "  - The issue was just timeout, not functionality" -ForegroundColor White
Write-Host "  - AI search is working correctly" -ForegroundColor White
Write-Host "  - The web portal is accessible on port 5000" -ForegroundColor White
Write-Host ""
Write-Host "If AI search still fails:" -ForegroundColor Yellow
Write-Host "  - There might be a deeper issue with the AI processing" -ForegroundColor White
Write-Host "  - Check LangChain service logs for errors" -ForegroundColor White
Write-Host "  - Verify Ollama is responding correctly" -ForegroundColor White 