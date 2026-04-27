using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.ValueObjects;

namespace CHAP2.Domain.Entities;

public class SetlistItem : IEquatable<SetlistItem>
{
    public Guid Id { get; private set; }
    public Guid SetlistId { get; private set; }
    public int Position { get; private set; }
    public SetlistItemKind Kind { get; private set; }

    public Guid? ChorusId { get; private set; }

    public string? BookId { get; private set; }
    public string? BookName { get; private set; }
    public int? Chapter { get; private set; }
    public int? Verse { get; private set; }
    public string? VerseText { get; private set; }
    public string? VerseRef { get; private set; }

    private SetlistItem() { }

    internal static SetlistItem Reconstitute(
        Guid id,
        Guid setlistId,
        int position,
        SetlistItemKind kind,
        Guid? chorusId,
        string? bookId,
        string? bookName,
        int? chapter,
        int? verse,
        string? verseText,
        string? verseRef)
    {
        return new SetlistItem
        {
            Id = id,
            SetlistId = setlistId,
            Position = position,
            Kind = kind,
            ChorusId = chorusId,
            BookId = bookId,
            BookName = bookName,
            Chapter = chapter,
            Verse = verse,
            VerseText = verseText,
            VerseRef = verseRef,
        };
    }

    internal static SetlistItem FromPayload(Guid setlistId, SetlistItemPayload payload, int position)
    {
        if (setlistId == Guid.Empty) throw new DomainException("Setlist ID cannot be empty.");
        if (payload is null) throw new DomainException("Item payload cannot be null.");
        if (position < 0) throw new DomainException("Position cannot be negative.");

        return new SetlistItem
        {
            Id = Guid.NewGuid(),
            SetlistId = setlistId,
            Position = position,
            Kind = payload.Kind,
            ChorusId = payload.ChorusId,
            BookId = payload.BookId,
            BookName = payload.BookName,
            Chapter = payload.Chapter,
            Verse = payload.Verse,
            VerseText = payload.VerseText,
            VerseRef = payload.VerseRef,
        };
    }

    internal void SetPosition(int position)
    {
        if (position < 0) throw new DomainException("Position cannot be negative.");
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
