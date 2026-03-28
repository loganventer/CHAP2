using CHAP2.Domain.Enums;
using CHAP2.Domain.ValueObjects;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.Events;

namespace CHAP2.Domain.Entities;

public class Chorus : IEquatable<Chorus>
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ChorusText { get; private set; } = string.Empty;
    public MusicalKey Key { get; private set; }
    public ChorusType Type { get; private set; }
    public TimeSignature TimeSignature { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public ChorusMetadata Metadata { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Chorus()
    {
        Metadata = new ChorusMetadata();
    }

    /// <summary>
    /// Reconstitutes a Chorus entity from persistence without raising domain events.
    /// Used by the repository/DTO layer for hydration from storage.
    /// </summary>
    internal static Chorus Reconstitute(
        Guid id,
        string name,
        string chorusText,
        MusicalKey key,
        ChorusType type,
        TimeSignature timeSignature,
        DateTime createdAt,
        DateTime? updatedAt,
        ChorusMetadata? metadata)
    {
        return new Chorus
        {
            Id = id,
            Name = name,
            ChorusText = chorusText,
            Key = key,
            Type = type,
            TimeSignature = timeSignature,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Metadata = metadata ?? new ChorusMetadata()
        };
    }

    public static Chorus Create(string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Chorus name cannot be empty.");
        
        if (string.IsNullOrWhiteSpace(chorusText))
            throw new DomainException("Chorus text cannot be empty.");
        
        if (key == MusicalKey.NotSet)
            throw new DomainException("Musical key must be specified.");
        
        if (type == ChorusType.NotSet)
            throw new DomainException("Chorus type must be specified.");
        
        if (timeSignature == TimeSignature.NotSet)
            throw new DomainException("Time signature must be specified.");

        var chorus = new Chorus
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ChorusText = chorusText.Trim(),
            Key = key,
            Type = type,
            TimeSignature = timeSignature,
            CreatedAt = DateTime.UtcNow,
            Metadata = new ChorusMetadata()
        };
        chorus._domainEvents.Add(new ChorusCreatedEvent(chorus.Id, chorus.Name));
        return chorus;
    }

    public static Chorus CreateFromSlide(string name, string chorusText, MusicalKey key = MusicalKey.NotSet)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Chorus name cannot be empty.");

        if (string.IsNullOrWhiteSpace(chorusText))
            throw new DomainException("Chorus text cannot be empty.");

        var chorus = new Chorus
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            ChorusText = chorusText.Trim(),
            Key = key,
            Type = ChorusType.NotSet,
            TimeSignature = TimeSignature.NotSet,
            CreatedAt = DateTime.UtcNow,
            Metadata = new ChorusMetadata()
        };
        chorus._domainEvents.Add(new ChorusCreatedEvent(chorus.Id, chorus.Name));
        return chorus;
    }

    public void Update(string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Chorus name cannot be empty.");

        if (string.IsNullOrWhiteSpace(chorusText))
            throw new DomainException("Chorus text cannot be empty.");

        // Allow NotSet values for imported choruses that haven't been fully categorized yet

        Name = name.Trim();
        ChorusText = chorusText.Trim();
        Key = key;
        Type = type;
        TimeSignature = timeSignature;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new ChorusUpdatedEvent(Id, Name));
    }

    public void UpdateMetadata(ChorusMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        UpdatedAt = DateTime.UtcNow;
    }

    public bool ContainsSearchTerm(string searchTerm, SearchScope scope = SearchScope.All)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return false;

        var term = searchTerm.ToLowerInvariant();
        
        return scope switch
        {
            SearchScope.Name => Name.ToLowerInvariant().Contains(term),
            SearchScope.Text => ChorusText.ToLowerInvariant().Contains(term),
            SearchScope.Key => Key.ToString().ToLowerInvariant().Contains(term),
            SearchScope.All => Name.ToLowerInvariant().Contains(term) || 
                              ChorusText.ToLowerInvariant().Contains(term) ||
                              Key.ToString().ToLowerInvariant().Contains(term),
            _ => false
        };
    }

    public bool Equals(Chorus? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Chorus);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(Chorus? left, Chorus? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Chorus? left, Chorus? right)
    {
        return !Equals(left, right);
    }
} 