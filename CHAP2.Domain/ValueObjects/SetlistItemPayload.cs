using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.ValueObjects;

public sealed class SetlistItemPayload
{
    public SetlistItemKind Kind { get; }
    public Guid? ChorusId { get; }
    public string? BookId { get; }
    public string? BookName { get; }
    public int? Chapter { get; }
    public int? Verse { get; }
    public string? VerseText { get; }
    public string? VerseRef { get; }

    private SetlistItemPayload(
        SetlistItemKind kind,
        Guid? chorusId,
        string? bookId,
        string? bookName,
        int? chapter,
        int? verse,
        string? verseText,
        string? verseRef)
    {
        Kind = kind;
        ChorusId = chorusId;
        BookId = bookId;
        BookName = bookName;
        Chapter = chapter;
        Verse = verse;
        VerseText = verseText;
        VerseRef = verseRef;
    }

    public static SetlistItemPayload ForChorus(Guid chorusId)
    {
        if (chorusId == Guid.Empty)
            throw new DomainException("Chorus ID cannot be empty.");
        return new SetlistItemPayload(SetlistItemKind.Chorus, chorusId, null, null, null, null, null, null);
    }

    public static SetlistItemPayload ForVerse(string bookId, string bookName, int chapter, int verse, string verseText, string verseRef)
    {
        if (string.IsNullOrWhiteSpace(bookId)) throw new DomainException("Verse book ID cannot be empty.");
        if (chapter <= 0) throw new DomainException("Verse chapter must be positive.");
        if (verse <= 0) throw new DomainException("Verse number must be positive.");
        return new SetlistItemPayload(
            SetlistItemKind.Verse,
            null,
            bookId.Trim(),
            string.IsNullOrWhiteSpace(bookName) ? bookId.Trim() : bookName.Trim(),
            chapter,
            verse,
            verseText ?? string.Empty,
            string.IsNullOrWhiteSpace(verseRef) ? $"{bookName ?? bookId} {chapter}:{verse}" : verseRef.Trim());
    }
}
