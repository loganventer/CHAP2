# CHAP2.Console.Common

A shared library containing common services and interfaces for CHAP2 console applications.

## Overview

This project provides reusable services and interfaces that are shared across multiple console applications in the CHAP2 ecosystem, promoting code reuse and maintaining consistency.

## Services

### ApiClientService
- **Interface**: `IApiClientService`
- **Purpose**: Handles HTTP communication with the CHAP2API
- **Features**:
  - Configurable base URL
  - JSON serialization/deserialization with case-insensitive property matching
  - Error handling and logging
  - HTTP client factory integration

### SearchService
- **Interface**: `ISearchService`
- **Purpose**: Provides search functionality for choruses
- **Features**:
  - Real-time search with configurable delays
  - Minimum search length validation
  - Search cancellation support
  - Result formatting with highlighted matches

### RegexHelperService
- **Interface**: `IRegexHelperService`
- **Purpose**: Provides regex functionality for search operations
- **Features**:
  - Case-insensitive regex matching
  - Reusable regex patterns
  - Performance optimization

### MemorySearchCacheService
- **Interface**: `ISearchCacheService`
- **Purpose**: Caches search results to reduce API calls
- **Features**:
  - 10-minute cache duration
  - Thread-safe operations
  - Configurable cache keys
  - Memory-efficient storage

## Interfaces

### ISearchResultsObserver
- **Purpose**: Observer pattern interface for search result updates
- **Usage**: Implemented by UI components to receive real-time search updates
- **Methods**:
  - `OnSearchResultsChanged()`: Called when search results change

## Configuration

The library supports configuration through `IConfiguration` for:
- API base URLs
- Search delays
- Minimum search lengths
- Cache durations

## Usage

```csharp
// Register services in DI container
services.AddSingleton<IApiClientService, ApiClientService>();
services.AddSingleton<ISearchService, SearchService>();
services.AddSingleton<IRegexHelperService, RegexHelperService>();
services.AddSingleton<ISearchCacheService, MemorySearchCacheService>();

// Use in console applications
var searchService = serviceProvider.GetRequiredService<ISearchService>();
var results = await searchService.SearchAsync("search term");
```

## Architecture Benefits

- **Code Reuse**: Shared services across multiple console applications
- **Consistency**: Standardized interfaces and implementations
- **Maintainability**: Single source of truth for common functionality
- **Testability**: Dependency injection enables easy unit testing
- **Performance**: Memory caching reduces API calls
- **Scalability**: Modular design allows easy extension

## Dependencies

- Microsoft.Extensions.Http
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging
- System.Text.Json 