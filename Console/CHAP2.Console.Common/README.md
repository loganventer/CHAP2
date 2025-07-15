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

### ConsoleDisplayService
- **Interface**: `IConsoleDisplayService`
- **Purpose**: Handles console UI display operations
- **Features**:
  - Chorus detail display formatting
  - Screen layout and positioning
  - Text normalization and wrapping
  - Console output management

### SelectionService
- **Interface**: `ISelectionService`
- **Purpose**: Manages selection state and navigation
- **Features**:
  - Selection index tracking
  - Navigation (up/down) logic
  - Number-based selection
  - Auto-selection for single results
  - Detail view state management

### ConsoleSearchResultsObserver
- **Interface**: `ISearchResultsObserver`
- **Purpose**: Observer pattern implementation for search result updates
- **Features**:
  - Real-time UI updates
  - Selection highlighting
  - Column-based result display
  - Context-aware text truncation

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
  - `OnResultsChanged()`: Called when search results change

### IConsoleDisplayService
- **Purpose**: Interface for console display operations
- **Usage**: Injected into console applications for UI operations
- **Methods**:
  - `DisplayChorus()`: Display single chorus
  - `DisplayChoruses()`: Display list of choruses
  - `DisplayChorusDetail()`: Display chorus in detail view
  - `ClearScreen()`, `SetCursorPosition()`, `WriteLine()`, `Write()`: Console I/O operations

### ISelectionService
- **Purpose**: Interface for selection state management
- **Usage**: Manages navigation and selection in console applications
- **Methods**:
  - `MoveUp()`, `MoveDown()`: Navigation
  - `SelectCurrent()`: Select current item
  - `TrySelectByNumber()`: Number-based selection
  - `UpdateTotalItems()`: Update available items count

## Configuration

The library supports configuration through `IConfiguration` for:
- API base URLs
- Search delays
- Minimum search lengths
- Cache durations

## Usage

```csharp
// Register services in DI container
services.AddScoped<IApiClientService, ApiClientService>();
services.AddScoped<IConsoleDisplayService, ConsoleDisplayService>();
services.AddScoped<ISelectionService, SelectionService>();
services.AddScoped<ISearchResultsObserver, ConsoleSearchResultsObserver>();
services.AddSingleton<ISearchCacheService, MemorySearchCacheService>();

// Use in console applications
var displayService = serviceProvider.GetRequiredService<IConsoleDisplayService>();
var selectionService = serviceProvider.GetRequiredService<ISelectionService>();
displayService.DisplayChorusDetail(chorus);
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