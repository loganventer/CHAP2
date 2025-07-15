# CHAP2.Application

The Application layer of the CHAP2 solution, implementing CQRS pattern with command and query services, orchestrating use cases, and dispatching domain events.

---

## 🏗️ Architecture Overview

This layer orchestrates the domain logic and coordinates between the domain and infrastructure layers. It implements the CQRS pattern, separating read and write operations, and handles domain event dispatching.

---

## 🧩 CQRS Implementation

### Command Services
- **IChorusCommandService**: Handles all write operations
  - `AddAsync()`: Create new choruses
  - `UpdateAsync()`: Update existing choruses
  - `DeleteAsync()`: Delete choruses
  - Domain event dispatching after state changes

### Query Services
- **IChorusQueryService**: Handles all read operations
  - `GetByIdAsync()`: Retrieve chorus by ID
  - `GetByNameAsync()`: Retrieve chorus by name
  - `GetAllAsync()`: Retrieve all choruses
  - `SearchAsync()`: Search choruses with various criteria

---

## 🎯 Use Case Orchestration

### Chorus Management
- **Create Chorus**: Validates input, creates domain entity, saves via repository, dispatches events
- **Update Chorus**: Validates changes, updates domain entity, persists changes
- **Delete Chorus**: Removes chorus from repository
- **Search Choruses**: Executes search queries with multiple criteria

### Slide Conversion
- **Convert Slide**: Processes PowerPoint files, extracts text, creates chorus with default metadata
- **Bulk Conversion**: Handles multiple file conversions with progress tracking

---

## 📡 Domain Event Dispatching

### Event Dispatcher
- **IDomainEventDispatcher**: Interface for dispatching domain events
- **DomainEventDispatcher**: Concrete implementation
- **Event Handling**: Dispatches events after successful state changes

### Domain Events
- **ChorusCreatedEvent**: Dispatched after successful chorus creation
- **Future Events**: Framework ready for additional domain events

---

## 📁 Project Structure

```
CHAP2.Application/
├── Services/
│   ├── ChorusCommandService.cs    # Command service implementation
│   ├── ChorusQueryService.cs      # Query service implementation
│   └── DomainEventDispatcher.cs   # Domain event dispatching
├── Interfaces/
│   ├── IChorusCommandService.cs   # Command service interface
│   ├── IChorusQueryService.cs     # Query service interface
│   ├── IChorusRepository.cs       # Repository interface
│   └── IDomainEventDispatcher.cs  # Event dispatcher interface
└── README.md                      # This file
```

---

## 🔧 Dependencies

- **CHAP2.Domain**: For domain entities, events, and business logic
- **CHAP2.Infrastructure**: For repository implementations (via interfaces)

---

## 🛠️ Extending the Application Layer

### Adding New CQRS Services
1. Create command/query service interfaces
2. Implement services with business logic
3. Register in DI container
4. Use repository interfaces for data access

### Adding New Use Cases
1. Define use case in appropriate service
2. Orchestrate domain logic
3. Handle domain events
4. Use repository pattern for data access

### Example CQRS Pattern
```csharp
// Command Service
public interface INewCommandService
{
    Task<Guid> CreateAsync(NewEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, NewEntity entity, CancellationToken cancellationToken = default);
}

// Query Service
public interface INewQueryService
{
    Task<NewEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<NewEntity>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

---

## 🏆 Benefits

- **CQRS**: Clear separation of read and write operations
- **Domain Events**: Event-driven architecture for loose coupling
- **Orchestration**: Coordinates between domain and infrastructure
- **Testability**: Easy to mock and test business logic
- **Maintainability**: Clear separation of concerns
- **Scalability**: Can optimize read and write operations independently 