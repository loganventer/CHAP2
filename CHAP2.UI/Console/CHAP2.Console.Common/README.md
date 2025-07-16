# CHAP2.Console.Common

A shared library containing common services and interfaces for CHAP2 console applications, supporting CQRS and domain event patterns.

## Overview

This project provides reusable services and interfaces shared across multiple console applications in the CHAP2 ecosystem, promoting code reuse, CQRS, and consistency.

## CQRS & Domain Events
- **CQRS**: Shared services (e.g., `ApiClientService`) are used by console apps to interact with the API's query and command endpoints.
- **Domain Events**: While domain events are primarily handled in the API, this library is designed to support event-driven patterns in the future.

## Services

### ApiClientService
- **Interface**: `IApiClientService`
- **Purpose**: Handles HTTP communication with the CHAP2.Chorus.Api (query and command endpoints)
- **Used by**: SearchConsole, BulkConsole

### ConsoleDisplayService
- **Interface**: `IConsoleDisplayService`
- **Purpose**: Handles console UI display operations
- **Used by**: SearchConsole, BulkConsole

### SelectionService
- **Interface**: `ISelectionService`
- **Purpose**: Manages selection state and navigation
- **Used by**: SearchConsole

### ConsoleSearchResultsObserver
- **Interface**: `ISearchResultsObserver`
- **Purpose**: Observer pattern implementation for search result updates
- **Used by**: SearchConsole

### MemorySearchCacheService
- **Interface**: `ISearchCacheService`
- **Purpose**: Caches search results to reduce API calls
- **Used by**: SearchConsole

## Interfaces & Usage
- **ISearchResultsObserver**: For observer-based UI updates (SearchConsole)
- **IConsoleDisplayService**: For all console UI operations (all console apps)
- **ISelectionService**: For selection/navigation (SearchConsole)

## Extending This Library
- Add new shared services/interfaces in this project for use by any console app.
- Register new services in the DI container of each console app.
- Example: To add a new logging or analytics service, define the interface and implementation here, then inject it where needed.

## Configuration
- Supports configuration through `IConfiguration` for API base URLs, search delays, cache durations, etc.

## Architecture Benefits
- **CQRS Support**: Enables console apps to use query/command endpoints cleanly.
- **Code Reuse**: Shared services across multiple console applications
- **Consistency**: Standardized interfaces and implementations
- **Maintainability**: Single source of truth for common functionality
- **Testability**: Dependency injection enables easy unit testing
- **Performance**: Memory caching reduces API calls
- **Scalability**: Modular design allows easy extension 