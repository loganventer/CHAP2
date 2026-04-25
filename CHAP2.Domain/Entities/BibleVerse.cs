using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

public sealed class BibleVerse : IEquatable<BibleVerse>
{
    public string BookId { get; }
    public string BookName { get; }
    public int Chapter { get; }
    public int Verse { get; }
    public string Text { get; }

    public BibleVerse(string bookId, string bookName, int chapter, int verse, string text)
    {
        if (string.IsNullOrWhiteSpace(bookId))
            throw new DomainException("BibleVerse bookId is required.");
        if (string.IsNullOrWhiteSpace(bookName))
            throw new DomainException("BibleVerse bookName is required.");
        if (chapter < 1)
            throw new DomainException("BibleVerse chapter must be >= 1.");
        if (verse < 1)
            throw new DomainException("BibleVerse verse must be >= 1.");
        if (text is null)
            throw new DomainException("BibleVerse text is required.");

        BookId = bookId;
        BookName = bookName;
        Chapter = chapter;
        Verse = verse;
        Text = text;
    }

    public bool Equals(BibleVerse? other) =>
        other is not null && BookId == other.BookId && Chapter == other.Chapter && Verse == other.Verse;

    public override bool Equals(object? obj) => Equals(obj as BibleVerse);
    public override int GetHashCode() => HashCode.Combine(BookId, Chapter, Verse);
}
