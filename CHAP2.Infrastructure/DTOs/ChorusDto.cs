using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Infrastructure.DTOs;

public class ChorusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChorusText { get; set; } = string.Empty;
    public MusicalKey Key { get; set; }
    public ChorusType Type { get; set; }
    public TimeSignature TimeSignature { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ChorusMetadata Metadata { get; set; } = new();
    public List<object> DomainEvents { get; set; } = new(); // For backward compatibility

    public static ChorusDto FromEntity(Chorus chorus)
    {
        return new ChorusDto
        {
            Id = chorus.Id,
            Name = chorus.Name,
            ChorusText = chorus.ChorusText,
            Key = chorus.Key,
            Type = chorus.Type,
            TimeSignature = chorus.TimeSignature,
            CreatedAt = chorus.CreatedAt,
            UpdatedAt = chorus.UpdatedAt,
            Metadata = chorus.Metadata
        };
    }

    public Chorus ToEntity()
    {
        // Check if musical properties are set, otherwise use CreateFromSlide
        Chorus chorus;
        if (Key == MusicalKey.NotSet || Type == ChorusType.NotSet || TimeSignature == TimeSignature.NotSet)
        {
            // Use CreateFromSlide for legacy data or incomplete data
            chorus = Chorus.CreateFromSlide(Name, ChorusText);
        }
        else
        {
            // Use Create for complete data with all musical properties
            chorus = Chorus.Create(Name, ChorusText, Key, Type, TimeSignature);
        }
        
        // Use reflection to set the properties that can't be set through public methods
        var type = typeof(Chorus);
        
        // Set ID (override the new ID with the original one)
        var idProperty = type.GetProperty(nameof(Id));
        idProperty?.SetValue(chorus, Id);
        
        // Set musical properties if they're not NotSet (for CreateFromSlide case)
        if (Key != MusicalKey.NotSet)
        {
            var keyProperty = type.GetProperty(nameof(Key));
            keyProperty?.SetValue(chorus, Key);
        }
        
        if (Type != ChorusType.NotSet)
        {
            var typeProperty = type.GetProperty(nameof(Type));
            typeProperty?.SetValue(chorus, Type);
        }
        
        if (TimeSignature != TimeSignature.NotSet)
        {
            var timeSignatureProperty = type.GetProperty(nameof(TimeSignature));
            timeSignatureProperty?.SetValue(chorus, TimeSignature);
        }
        
        // Set timestamps
        var createdAtProperty = type.GetProperty(nameof(CreatedAt));
        createdAtProperty?.SetValue(chorus, CreatedAt);
        
        if (UpdatedAt.HasValue)
        {
            var updatedAtProperty = type.GetProperty(nameof(UpdatedAt));
            updatedAtProperty?.SetValue(chorus, UpdatedAt);
        }
        
        // Set metadata
        var metadataProperty = type.GetProperty(nameof(Metadata));
        metadataProperty?.SetValue(chorus, Metadata);
        
        return chorus;
    }
} 