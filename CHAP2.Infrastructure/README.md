# CHAP2.Infrastructure

The Infrastructure layer of the CHAP2 solution, responsible for data access, persistence, and external service implementations.

---

## 🏗️ Architecture Overview

This layer implements the interfaces defined in the Application layer, providing concrete implementations for data access and external services while maintaining clean architecture principles.

---

## 📊 Data Persistence Strategy

### DTO Pattern
- **ChorusDto**: Data Transfer Object for JSON serialization/deserialization
- **Domain Purity**: Keeps domain entities clean and focused on business logic
- **Backward Compatibility**: Handles legacy JSON format while maintaining clean domain model

### JSON Serialization Strategy
- **File Storage**: Each chorus stored as individual JSON file with GUID-based naming
- **Metadata Handling**: Musical key, type, and time signature properly serialized with enum values
- **Legacy Support**: JSON converters handle old data format without breaking domain model

---

## 🗂️ Repository Implementations

### DiskChorusRepository
- **Interface**: `IChorusRepository`
- **Storage**: File-based JSON storage in `data/chorus/` directory
- **Methods**: `GetByIdAsync`, `GetByNameAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- **Naming**: Consistent async method naming across all repositories
- **DTO Usage**: Uses `ChorusDto` for serialization/deserialization

### Key Features
- **GUID-based Naming**: Each chorus file named with its GUID for uniqueness
- **Error Handling**: Comprehensive error handling for file operations
- **Async Operations**: All operations are async for better performance
- **Domain Mapping**: Converts between DTOs and domain entities

---

## 📁 Project Structure

```
CHAP2.Infrastructure/
├── DTOs/
│   └── ChorusDto.cs          # Data Transfer Object for JSON serialization
├── Repositories/
│   └── DiskChorusRepository.cs # File-based repository implementation
└── README.md                  # This file
```

---

## 🔧 Dependencies

- **CHAP2.Domain**: For domain entities, enums, and value objects
- **CHAP2.Application**: For repository interfaces and domain events
- **System.Text.Json**: For JSON serialization/deserialization

---

## 🛠️ Extending the Infrastructure

### Adding New Repositories
1. Create new repository class implementing the appropriate interface
2. Follow consistent async naming conventions
3. Use DTO pattern for data persistence if needed
4. Register in DI container

### Adding New DTOs
1. Create DTO class in `DTOs/` directory
2. Include mapping methods to/from domain entities
3. Handle backward compatibility if needed

### Example Repository Pattern
```csharp
public class NewRepository : INewRepository
{
    public async Task<Entity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Implementation
    }
    
    public async Task<Entity> AddAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

---

## 🏆 Benefits

- **Clean Architecture**: Implements interfaces from Application layer
- **Domain Purity**: DTO pattern keeps domain entities clean
- **Backward Compatibility**: Handles legacy data formats
- **Testability**: Easy to mock and test
- **Maintainability**: Clear separation of concerns
- **Performance**: Async operations and efficient data handling 