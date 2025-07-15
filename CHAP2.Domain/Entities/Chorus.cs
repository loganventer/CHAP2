using CHAP2.Domain.Enums;
using CHAP2.Domain.ValueObjects;
using CHAP2.Domain.Exceptions;
using System.Collections.Generic;
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
    public List<IDomainEvent> DomainEvents { get; } = new();

    // Private constructor for EF Core and internal use
    private Chorus() 
    {
        Metadata = new ChorusMetadata();
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
        chorus.DomainEvents.Add(new ChorusCreatedEvent(chorus.Id, chorus.Name));
        return chorus;
    }

    public void Update(string name, string chorusText, MusicalKey key, ChorusType type, TimeSignature timeSignature)
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

        Name = name.Trim();
        ChorusText = chorusText.Trim();
        Key = key;
        Type = type;
        TimeSignature = timeSignature;
        UpdatedAt = DateTime.UtcNow;
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