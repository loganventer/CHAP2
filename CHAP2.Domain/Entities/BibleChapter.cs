using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

public sealed class BibleChapter
{
    public BibleBook Book { get; }
    public int Number { get; }
    public IReadOnlyList<BibleVerse> Verses { get; }

    public BibleChapter(BibleBook book, int number, IReadOnlyList<BibleVerse> verses)
    {
        if (book is null)
            throw new DomainException("BibleChapter requires a book.");
        if (number < 1 || number > book.ChapterCount)
            throw new DomainException($"BibleChapter number {number} out of range for {book.Name} (1-{book.ChapterCount}).");
        if (verses is null || verses.Count == 0)
            throw new DomainException("BibleChapter requires at least one verse.");

        Book = book;
        Number = number;
        Verses = verses;
    }
}
