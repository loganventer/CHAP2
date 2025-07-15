# CHAP2 - Musical Chorus Management System

A .NET solution for managing musical choruses, following **IDesign Clean Architecture** and CQRS principles. Features a Web API, interactive search console, and bulk slide conversion tools.

---

## 🏗️ Solution Structure

```
CHAP2/
├── CHAP2.Domain/           # Core business entities, enums, value objects, domain events
├── CHAP2.Application/      # Application services (CQRS: Query/Command), interfaces
├── CHAP2.Infrastructure/   # Data access, repository implementations, DTOs
├── CHAP2.Shared/           # DTOs and shared utilities
├── CHAP2.Chorus.Api/       # Web API (REST, CQRS, DI, OpenAPI)
└── Console/
    ├── CHAP2.Console.Common/    # Shared console services
    ├── CHAP2.SearchConsole/     # Interactive search console
    └── CHAP2.Console.Bulk/      # Bulk conversion console
```

---

## 🧩 Architectural Highlights

- **Clean Architecture**: Clear separation of Domain, Application, Infrastructure, and Presentation layers.
- **CQRS**: Command and Query responsibilities are split into `IChorusCommandService` and `IChorusQueryService`.
- **Domain Events**: Business events (e.g., `ChorusCreatedEvent`) are raised and dispatched via `IDomainEventDispatcher`.
- **Repository Pattern**: Consistent naming (`GetByIdAsync`, `AddAsync`, etc.) and abstraction for data access.
- **Domain Factory Methods**: `Chorus.Create()` for manual creation, `Chorus.CreateFromSlide()` for slide conversions.
- **DTO Pattern**: `ChorusDto` in Infrastructure layer for JSON serialization/deserialization, preserving domain purity.
- **Backward Compatibility**: JSON converters handle legacy data format while maintaining clean domain model.
- **Dependency Injection**: All services and repositories are registered via DI for testability and flexibility.
- **Testability**: All business logic is in the Application and Domain layers, easily unit tested.

---

## 🚀 How to Build & Run

### Prerequisites
- .NET 9.0 SDK

### Build Everything
```bash
dotnet build
```

### Run the API
```bash
cd CHAP2.Chorus.Api
dotnet run
```

### Run the Search Console
```bash
cd Console/CHAP2.SearchConsole
dotnet run
```

### Run the Bulk Console
```bash
cd Console/CHAP2.Console.Bulk
dotnet run
```

---

## 🧠 CQRS & Domain Events

- **CQRS**: All read operations go through `IChorusQueryService`, all write operations through `IChorusCommandService`.
- **Domain Events**: Domain events (e.g., `ChorusCreatedEvent`) are raised in the domain model and dispatched by the application layer.
- **Repository Naming**: All repositories use consistent async method names: `GetByIdAsync`, `GetByNameAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, etc.

---

## 📊 Data Persistence & JSON Strategy

- **Domain Purity**: Domain entities remain clean and focused on business logic.
- **DTO Pattern**: `ChorusDto` in Infrastructure layer handles JSON serialization/deserialization.
- **Backward Compatibility**: JSON converters handle legacy data format while maintaining clean domain model.
- **File Storage**: Each chorus stored as individual JSON file with GUID-based naming.
- **Metadata Handling**: Musical key, type, and time signature properly serialized with enum values.

---

## 📝 API Endpoints (CHAP2.Chorus.Api)
- `POST   /api/choruses`              # Add new chorus
- `GET    /api/choruses`              # Get all choruses
- `GET    /api/choruses/{id}`         # Get chorus by ID
- `PUT    /api/choruses/{id}`         # Update chorus
- `DELETE /api/choruses/{id}`         # Delete chorus
- `GET    /api/choruses/search`       # Search choruses
- `GET    /api/choruses/by-name/{name}` # Get chorus by name
- `POST   /api/slide/convert`         # Convert PowerPoint file to chorus
- `GET    /api/health/ping`           # Health check

---

## 🛠️ Console Apps
- **CHAP2.SearchConsole**: Real-time, interactive search with observer-based UI and memory cache.
- **CHAP2.Console.Bulk**: Batch PowerPoint-to-chorus conversion with progress tracking.
- **CHAP2.Console.Common**: Shared services for API communication, display, and selection logic.

---

## 🧪 Testing
- Use `.http` files in `CHAP2.Chorus.Api/.http/` for API endpoint testing.
- Console apps provide interactive/manual testing for search and bulk conversion.

---

## 📚 Extending the Solution
- Add new domain logic in `CHAP2.Domain`.
- Add new use cases/services in `CHAP2.Application` (implement CQRS interfaces).
- Add new data sources in `CHAP2.Infrastructure` (implement repository interfaces).
- Add new endpoints/controllers in `CHAP2.Chorus.Api` (use DI and CQRS services).
- Add new console tools in `Console/` (reuse `CHAP2.Console.Common`).

---

## 🏆 IDesign & Clean Architecture Benefits
- **Separation of Concerns**
- **Explicit Contracts**
- **Testability**
- **Maintainability**
- **Scalability**
- **Performance** 