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
        // Use the internal Reconstitute method to hydrate the entity without reflection
        return Chorus.Reconstitute(
            Id,
            Name,
            ChorusText,
            Key,
            Type,
            TimeSignature,
            CreatedAt,
            UpdatedAt,
            Metadata);
    }
}
