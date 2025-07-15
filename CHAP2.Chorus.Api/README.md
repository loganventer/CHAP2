# CHAP2.Chorus.Api

A .NET Web API for managing musical choruses, following **IDesign Clean Architecture** and CQRS principles.

---

## 🏗️ Architecture Overview

- **Domain Layer**: Rich entities, enums, value objects, and domain events (e.g., `ChorusCreatedEvent`).
- **Application Layer**: CQRS services (`IChorusQueryService`, `IChorusCommandService`), orchestrating use cases and dispatching domain events.
- **Infrastructure Layer**: Repository implementations (e.g., `DiskChorusRepository`) with consistent async method naming.
- **API Layer**: Controllers delegate to application services, use DI, and support async/cancellation.

---

## 🚦 CQRS & Domain Events

- **CQRS**: All reads go through `IChorusQueryService`, all writes through `IChorusCommandService`.
- **Domain Events**: Raised in the domain model and dispatched by `IDomainEventDispatcher` after state changes.
- **Repository Naming**: All repositories use `GetByIdAsync`, `GetByNameAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`, etc.

---

## 📝 API Endpoints
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

## 🧩 Dependency Injection
- All services, repositories, and event dispatchers are registered in `Program.cs`.
- Controllers receive dependencies via constructor injection.

---

## 🧪 Testing
- Use `.http` files in `.http/` for endpoint testing (CRUD, search, health, slide conversion).
- Swagger UI available at `/swagger` when running locally.

---

## 🛠️ Extending the API
- Add new domain logic in `CHAP2.Domain`.
- Add new CQRS services in `CHAP2.Application`.
- Implement new repositories in `CHAP2.Infrastructure`.
- Register new services in `Program.cs`.
- Add new controllers in `CHAP2.Chorus.Api/Controllers` (use DI and CQRS services).

---

## 🏆 Benefits
- **Separation of Concerns**
- **Explicit Contracts**
- **Testability**
- **Maintainability**
- **Scalability**
- **Performance** 