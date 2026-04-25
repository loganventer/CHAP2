using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

public sealed class BibleBook : IEquatable<BibleBook>
{
    public string Id { get; }
    public string Name { get; }
    public string EnglishName { get; }
    public int Ordinal { get; }
    public BibleTestament Testament { get; }
    public int ChapterCount { get; }

    public BibleBook(string id, string name, string englishName, int ordinal, BibleTestament testament, int chapterCount)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("BibleBook id is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("BibleBook name is required.");
        if (string.IsNullOrWhiteSpace(englishName))
            throw new DomainException("BibleBook englishName is required.");
        if (ordinal is < 1 or > 66)
            throw new DomainException("BibleBook ordinal must be between 1 and 66.");
        if (chapterCount < 1)
            throw new DomainException("BibleBook chapterCount must be >= 1.");

        Id = id;
        Name = name;
        EnglishName = englishName;
        Ordinal = ordinal;
        Testament = testament;
        ChapterCount = chapterCount;
    }

    public bool Equals(BibleBook? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as BibleBook);
    public override int GetHashCode() => Id.GetHashCode();
}
