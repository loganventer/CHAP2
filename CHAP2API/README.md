# CHAP2API

A .NET Web API project for managing musical choruses with clean architecture and dependency injection.

## Project Structure

```
CHAP2API/
├── Controllers/
│   ├── ChapControllerAbstractBase.cs    # Abstract base controller
│   ├── ChorusesController.cs            # Chorus management endpoints
│   ├── HealthController.cs              # Health monitoring endpoints
│   └── SlideController.cs               # Slide management (placeholder)
├── Interfaces/
│   ├── IController.cs                   # Base controller interface
│   └── IServices.cs                     # Application services interface
├── Services/
│   └── Services.cs                      # Implementation of IServices
├── .http/                               # HTTP test files
│   ├── choruses.http                    # Chorus endpoint tests
│   ├── health.http                      # Health endpoint tests
│   ├── slide.http                       # Slide endpoint tests
│   └── README.md                        # Testing documentation
├── Program.cs                           # Application entry point with DI configuration
├── GlobalRoutePrefixConvention.cs       # Global route prefix configuration
└── README.md                            # This file
```

## Architecture

The project follows clean architecture principles with proper separation of concerns:

### Domain Models (CHAP2.Common)
- **Models/** - Domain entities (Chorus)
- **Enum/** - Enumerations (MusicalKey, TimeSignature, ChorusType)
- **Interfaces/** - Domain interfaces (IChorusResource)
- **Resources/** - Data access implementations

### API Layer (CHAP2API)
- **Custom Controller Base** - All controllers inherit from `ChapControllerAbstractBase`
- **Global Route Prefix** - All endpoints prefixed with `/api`
- **Dependency Injection** - Properly configured services

## Controllers

### ChorusesController
Manages musical chorus data with full CRUD operations:
- `POST /api/choruses` - Add a new chorus
- `GET /api/choruses` - Get all choruses

### HealthController
Provides health monitoring endpoints:
- `GET /api/health/ping` - Health check endpoint

### SlideController
Placeholder for future slide management functionality.

## API Endpoints

### Chorus Management
- `POST /api/choruses` - Add a new chorus
- `GET /api/choruses` - Get all choruses

### Health Monitoring
- `GET /api/health/ping` - Health check

## Running the Application

1. Navigate to the project directory:
   ```bash
   cd CHAP2API
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. The API will be available at `http://localhost:5000`

4. Swagger documentation will be available at `http://localhost:5000/swagger`

## Testing

Use the provided HTTP files in the `.http/` folder to test the API:

- `.http/choruses.http` - Chorus-specific tests
- `.http/health.http` - Health endpoint tests
- `.http/slide.http` - Slide endpoint tests

## Adding New Controllers

To add new controllers:

1. Create a new controller that inherits from `ChapControllerAbstractBase`
2. Implement the `IController` interface
3. Use the `[ApiController]` and `[Route("[controller]")]` attributes
4. Add logging using the `LogAction` method

Example:
```csharp
[ApiController]
[Route("[controller]")]
public class MyController : ChapControllerAbstractBase, IController
{
    public MyController(ILogger<MyController> logger) 
        : base(logger)
    {
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        LogAction("Get");
        return Ok("Hello World");
    }
}
```

## Adding New Services

To add new services:

1. Create a new interface in the `Interfaces/` folder
2. Create an implementation in the `Services/` folder
3. Register the service in `Program.cs`
4. Inject the service in your controllers

Example:
```csharp
// In Program.cs
builder.Services.AddScoped<IMyService, MyService>();

// In Controller
public class MyController : ChapControllerAbstractBase
{
    private readonly IMyService _myService;
    
    public MyController(ILogger<MyController> logger, IMyService myService)
        : base(logger)
    {
        _myService = myService;
    }
}
``` 