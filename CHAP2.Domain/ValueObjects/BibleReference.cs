using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.ValueObjects;

public sealed class BibleReference : IEquatable<BibleReference>
{
    public string BookId { get; }
    public int Chapter { get; }
    public int? Verse { get; }

    public BibleReference(string bookId, int chapter, int? verse = null)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new DomainException("BibleReference requires a bookId.");
        if (chapter < 1)
            throw new DomainException("BibleReference chapter must be >= 1.");
        if (verse.HasValue && verse.Value < 1)
            throw new DomainException("BibleReference verse must be >= 1 when provided.");

        BookId = bookId.Trim().ToLowerInvariant();
        Chapter = chapter;
        Verse = verse;
    }

    public bool Equals(BibleReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return BookId == other.BookId && Chapter == other.Chapter && Verse == other.Verse;
    }

    public override bool Equals(object? obj) => Equals(obj as BibleReference);

    public override int GetHashCode() => HashCode.Combine(BookId, Chapter, Verse);

    public override string ToString() =>
        Verse.HasValue ? $"{BookId} {Chapter}:{Verse}" : $"{BookId} {Chapter}";

    public static bool operator ==(BibleReference? left, BibleReference? right) => Equals(left, right);
    public static bool operator !=(BibleReference? left, BibleReference? right) => !Equals(left, right);
}
