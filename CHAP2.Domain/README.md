# CHAP2.Domain

The Domain layer of the CHAP2 solution, containing the core business entities, value objects, domain events, and business logic.

---

## 🏗️ Architecture Overview

This is the heart of the application, containing all business logic, entities, and domain rules. It has no dependencies on other layers and represents the core domain model.

---

## 🧩 Core Components

### Entities
- **Chorus**: The main domain entity representing a musical chorus
  - Rich domain model with business logic
  - Factory methods: `Create()` and `CreateFromSlide()`
  - Domain events: `ChorusCreatedEvent`

### Value Objects
- **ChorusMetadata**: Encapsulates musical metadata (key, type, time signature)
  - Immutable and self-validating
  - JSON serialization support with backward compatibility

### Domain Events
- **ChorusCreatedEvent**: Raised when a new chorus is created
  - Contains chorus ID and metadata
  - Dispatched by application layer

### Enums
- **ChorusType**: Defines different types of choruses
- **MusicalKey**: Represents musical keys
- **TimeSignature**: Represents time signatures

### Exceptions
- **DomainException**: Base exception for domain rule violations
- **ChorusValidationException**: Specific exception for chorus validation errors

---

## 🏭 Factory Methods

### Chorus.Create()
- **Purpose**: Manual creation of choruses with full metadata
- **Usage**: When all musical metadata is known
- **Validation**: Ensures all required fields are provided

### Chorus.CreateFromSlide()
- **Purpose**: Creation from PowerPoint slide conversion
- **Usage**: When converting slides with unknown metadata
- **Default Values**: Uses "NotSet" values for unknown musical properties

---

## 📁 Project Structure

```
CHAP2.Domain/
├── Entities/
│   └── Chorus.cs              # Main domain entity
├── ValueObjects/
│   └── ChorusMetadata.cs      # Musical metadata value object
├── Events/
│   └── ChorusCreatedEvent.cs  # Domain events
├── Enums/
│   ├── ChorusType.cs          # Chorus type enumeration
│   ├── MusicalKey.cs          # Musical key enumeration
│   └── TimeSignature.cs       # Time signature enumeration
├── Exceptions/
│   └── DomainException.cs     # Domain exceptions
└── README.md                  # This file
```

---

## 🔧 Domain Rules

### Chorus Validation
- **Name**: Required, non-empty string
- **Text**: Required, non-empty string
- **Metadata**: Optional, but must be valid when provided
- **Uniqueness**: Names must be unique (case-insensitive)

### Metadata Validation
- **Key**: Must be a valid musical key or NotSet
- **Type**: Must be a valid chorus type or NotSet
- **Time Signature**: Must be a valid time signature or NotSet

---

## 🛠️ Extending the Domain

### Adding New Entities
1. Create entity class with business logic
2. Add factory methods if needed
3. Include domain events for important state changes
4. Add validation rules

### Adding New Value Objects
1. Create immutable value object class
2. Include validation logic
3. Override equality methods
4. Add JSON serialization if needed

### Adding New Domain Events
1. Create event class inheriting from `IDomainEvent`
2. Include relevant data for event handlers
3. Raise events in entity methods

### Example Entity Pattern
```csharp
public class NewEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    
    private NewEntity() { } // For EF Core
    
    public static NewEntity Create(string name)
    {
        // Validation and creation logic
    }
    
    public void UpdateName(string newName)
    {
        // Business logic and validation
    }
}
```

---

## 🏆 Benefits

- **Clean Architecture**: No dependencies on other layers
- **Business Logic**: All domain rules encapsulated here
- **Testability**: Easy to unit test business logic
- **Maintainability**: Clear separation of concerns
- **Domain Events**: Enables event-driven architecture
- **Factory Methods**: Ensures valid entity creation 