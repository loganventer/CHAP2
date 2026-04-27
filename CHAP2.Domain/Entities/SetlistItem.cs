using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

public class SetlistItem : IEquatable<SetlistItem>
{
    public Guid Id { get; private set; }
    public Guid SetlistId { get; private set; }
    public Guid ChorusId { get; private set; }
    public int Position { get; private set; }

    private SetlistItem() { }

    internal static SetlistItem Reconstitute(Guid id, Guid setlistId, Guid chorusId, int position)
    {
        return new SetlistItem
        {
            Id = id,
            SetlistId = setlistId,
            ChorusId = chorusId,
            Position = position,
        };
    }

    internal static SetlistItem Create(Guid setlistId, Guid chorusId, int position)
    {
        if (setlistId == Guid.Empty)
            throw new DomainException("Setlist ID cannot be empty.");
        if (chorusId == Guid.Empty)
            throw new DomainException("Chorus ID cannot be empty.");
        if (position < 0)
            throw new DomainException("Position cannot be negative.");

        return new SetlistItem
        {
            Id = Guid.NewGuid(),
            SetlistId = setlistId,
            ChorusId = chorusId,
            Position = position,
        };
    }

    internal void SetPosition(int position)
    {
        if (position < 0)
            throw new DomainException("Position cannot be negative.");
        Position = position;
    }

    public bool Equals(SetlistItem? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as SetlistItem);

    public override int GetHashCode() => Id.GetHashCode();
}
