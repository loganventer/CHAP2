#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Simple test script for AI search on Windows
.DESCRIPTION
    Tests the AI search functionality to verify it's working correctly
#>

Write-Host "=== Testing AI Search on Windows ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if containers are running
Write-Host "Test 1: Checking containers..." -ForegroundColor Yellow
try {
    $containers = docker-compose ps --format json | ConvertFrom-Json
    $runningCount = ($containers | Where-Object { $_.State -eq "running" }).Count
    Write-Host "   Running containers: $runningCount/5" -ForegroundColor Green
    
    if ($runningCount -lt 5) {
        Write-Host "   WARNING: Not all containers are running" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ERROR: Could not check containers" -ForegroundColor Red
}

# Test 2: Test AI search endpoint
Write-Host "Test 2: Testing AI search endpoint..." -ForegroundColor Yellow
try {
    $body = @{
        query = "Jesus"
        maxResults = 3
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/IntelligentSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 10
    
    if ($response.searchResults -and $response.searchResults.Count -gt 0) {
        Write-Host "   PASS: AI search returned $($response.searchResults.Count) results" -ForegroundColor Green
        Write-Host "   First result: $($response.searchResults[0].name)" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: No results returned" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: AI search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Test RAG search endpoint
Write-Host "Test 3: Testing RAG search endpoint..." -ForegroundColor Yellow
try {
    $body = @{
        query = "What are some songs about love?"
        maxResults = 3
    } | ConvertTo-Json

    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/RagSearch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 15
    
    if ($response.response -and $response.response.Length -gt 0) {
        Write-Host "   PASS: RAG search returned response" -ForegroundColor Green
        Write-Host "   Response length: $($response.response.Length) characters" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: No RAG response" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: RAG search failed - $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Test traditional search
Write-Host "Test 4: Testing traditional search..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5002/Home/Search?q=Jesus" -Method Get -TimeoutSec 5
    
    if ($response.results -and $response.results.Count -gt 0) {
        Write-Host "   PASS: Traditional search returned $($response.results.Count) results" -ForegroundColor Green
    } else {
        Write-Host "   FAIL: No traditional search results" -ForegroundColor Red
    }
} catch {
    Write-Host "   FAIL: Traditional search failed - $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "If any tests failed, try:" -ForegroundColor Yellow
Write-Host "1. docker-compose restart" -ForegroundColor White
Write-Host "2. Check logs: docker-compose logs chap2-webportal" -ForegroundColor White
Write-Host "3. Check logs: docker-compose logs langchain-service" -ForegroundColor White 