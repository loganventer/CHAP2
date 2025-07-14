# CHAP2 - Musical Chorus Management System

A .NET solution for managing musical choruses with clean architecture, featuring a Web API, interactive search console, and comprehensive slide conversion capabilities.

## Solution Architecture

```
CHAP2/
├── CHAP2.Common/                    # Shared domain models and services
│   ├── Models/                      # Domain entities
│   ├── Enum/                        # Enumerations
│   ├── Interfaces/                  # Domain interfaces
│   ├── Services/                    # Business logic services
│   └── Resources/                   # Data access implementations
├── CHAP2API/                        # Web API layer
│   ├── Controllers/                 # API endpoints
│   ├── Services/                    # Application services
│   └── Interfaces/                  # API-specific interfaces
├── CHAP2.SearchConsole/             # Interactive search console
└── .vscode/                         # VS Code configuration
```

## Projects

### CHAP2.Common
**Domain Layer** - Contains all shared business logic, models, and interfaces.

#### Key Components:
- **Models/Chorus.cs** - Core domain entity with GUID identification
- **Enum/** - Musical enums (MusicalKey, TimeSignature, ChorusType) with NotSet defaults
- **Interfaces/** - Domain contracts (IChorusResource, ISearchService, IApiClientService, IRegexHelperService)
- **Services/** - Business logic implementations (SearchService, SlideToChorusService, ApiClientService, RegexHelperService)
- **Resources/** - Data access (DiskChorusResource with GUID-based file storage)

#### Architecture Principles:
- **Clean Separation** - Domain logic separated from infrastructure
- **GUID-based Identification** - Unique identification for all choruses
- **Case-insensitive Validation** - Prevents duplicate names regardless of case
- **NotSet Defaults** - Enum properties default to NotSet (0) when not specified
- **Shared Utilities** - Common functionality like regex operations centralized

### CHAP2API
**API Layer** - RESTful Web API with clean controller architecture.

#### Key Features:
- **Custom Controller Base** - All controllers inherit from `ChapControllerAbstractBase`
- **Global Route Prefix** - All endpoints prefixed with `/api`
- **Dependency Injection** - Properly configured services
- **CancellationToken Support** - Full async cancellation support throughout

#### Controllers:
- **ChorusesController** - Full CRUD operations with comprehensive search
- **HealthController** - Health monitoring endpoints
- **SlideController** - PowerPoint slide conversion to chorus structure

#### API Endpoints:
```
POST   /api/choruses              # Add new chorus
GET    /api/choruses              # Get all choruses
GET    /api/choruses/{id}         # Get chorus by ID
PUT    /api/choruses/{id}         # Update chorus
GET    /api/choruses/search       # Search choruses (multiple modes)
GET    /api/choruses/by-name/{name} # Get chorus by exact name
POST   /api/slide/convert         # Convert PowerPoint file to chorus
GET    /api/health/ping           # Health check
```

### CHAP2.SearchConsole
**Console Application** - Interactive search and slide conversion tool.

#### Features:
- **Dual Mode Operation** - Search mode (default) and convert mode
- **Real-time Search** - Interactive search with configurable delays
- **Slide Conversion** - Convert .ppsx files to chorus structures
- **Service Layer Integration** - Uses shared services from CHAP2.Common

#### Usage:
```bash
# Interactive search (default)
dotnet run

# Convert a slide file
dotnet run convert path/to/file.ppsx

# Convert using configured default file
dotnet run convert
```

#### Configuration:
- **SearchDelayMs** - Delay before triggering search (default: 300ms)
- **MinSearchLength** - Minimum characters before searching (default: 2)
- **ApiBaseUrl** - API base URL (default: http://localhost:5000)
- **DefaultPpsxFilePath** - Default file for convert mode

## Service Layer Architecture

### IApiClientService
Centralized HTTP communication service providing:
- API connectivity testing
- Slide file conversion
- Chorus search operations
- CRUD operations for choruses

### IConsoleApplicationService
Business logic service for console applications providing:
- Interactive search functionality
- Slide conversion workflow
- Display formatting for choruses
- Error handling and user feedback

### IRegexHelperService
Shared regex utility service providing:
- Safe regex pattern matching with error handling
- Case-insensitive regex operations
- Pattern validation
- Match extraction functionality

## Key Features

### Search Functionality
- **Multiple Search Modes** - Exact, Contains, Regex
- **Search Scope** - Names, text, or both
- **Real-time Performance** - Optimized for responsive search
- **Cancellation Support** - Previous searches cancelled when new input arrives
- **Safe Regex Operations** - Centralized regex handling with error recovery

### Slide Conversion
- **PowerPoint Support** - .ppsx and .pptx files
- **Binary Processing** - Direct file-to-chorus conversion
- **No File Storage** - Converts directly to chorus structure
- **Error Handling** - Comprehensive validation and error reporting

### Data Management
- **GUID-based Storage** - Unique identification system
- **Duplicate Prevention** - Case-insensitive name validation
- **JSON Storage** - Human-readable file format
- **Backup Safety** - File-based storage with easy backup

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- VS Code with C# extension (recommended)

### Getting Started
1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd CHAP2
   ```

2. **Build the solution**
   ```bash
   dotnet build
   ```

3. **Run the API**
   ```bash
   cd CHAP2API
   dotnet run
   ```

4. **Run the search console**
   ```bash
   cd CHAP2.SearchConsole
   dotnet run
   ```

### VS Code Integration
- **Compound Launch** - Debug API and console simultaneously
- **Build Tasks** - Automatic building before launch
- **HTTP Testing** - Pre-configured test files in `.http/`

## Testing

### HTTP Tests
Use the provided HTTP files in `CHAP2API/.http/`:
- `choruses.http` - CRUD and search operations
- `health.http` - Health endpoint tests
- `slide.http` - Slide conversion tests

### Console Testing
The search console provides interactive testing:
- Real-time search with immediate feedback
- Slide conversion with result verification
- Error handling and connectivity testing

## Configuration

### API Configuration (`CHAP2API/appsettings.json`)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

### Console Configuration (`CHAP2.SearchConsole/appsettings.json`)
```json
{
  "ApiBaseUrl": "http://localhost:5000",
  "SearchDelayMs": 300,
  "MinSearchLength": 2,
  "DefaultPpsxFilePath": "./sample.ppsx"
}
```

## Architecture Benefits

### Clean Architecture
- **Separation of Concerns** - Clear boundaries between layers
- **Dependency Inversion** - High-level modules don't depend on low-level modules
- **Testability** - Easy to unit test with dependency injection
- **Maintainability** - Clear structure makes changes predictable

### Performance
- **Async/Await** - Non-blocking operations throughout
- **Cancellation Support** - Proper resource cleanup
- **Debounced Search** - Reduces API calls with configurable delays
- **Efficient Storage** - JSON-based file storage with GUID indexing
- **Safe Regex** - Centralized regex handling prevents crashes

### Scalability
- **Service Layer** - Easy to add new services
- **Interface-based Design** - Easy to swap implementations
- **Configuration-driven** - Environment-specific settings
- **Modular Structure** - Easy to add new features
- **Shared Utilities** - Common functionality centralized

## Contributing

### Adding New Features
1. **Domain Layer** - Add models/interfaces to CHAP2.Common
2. **Service Layer** - Implement business logic in services
3. **API Layer** - Add controllers and endpoints
4. **Console Layer** - Add console functionality if needed

### Code Standards
- **Clean Architecture** - Follow dependency inversion
- **Async/Await** - Use throughout for I/O operations
- **Cancellation Tokens** - Support cancellation in all async methods
- **Error Handling** - Comprehensive exception handling
- **Logging** - Structured logging with appropriate levels
- **Shared Utilities** - Use centralized helpers for common operations

## License

This project is licensed under the MIT License - see the LICENSE file for details. 