using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class BibleReferenceParserTests
{
    private IBibleBookRepository _books = null!;
    private BibleReferenceParser _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _books = Substitute.For<IBibleBookRepository>();
        // The full 66-book canon, only the slugs matter for the parser tests.
        _books.GetAllBooksAsync(Arg.Any<CancellationToken>()).Returns(new List<BibleBook>
        {
            new("genesis", "Genesis", "Genesis", 1, BibleTestament.Old, 50),
            new("psalms", "Psalms", "Psalms", 19, BibleTestament.Old, 150),
            new("johannes", "Johannes", "John", 43, BibleTestament.New, 21),
            new("1-johannes", "1 Johannes", "1 John", 62, BibleTestament.New, 5),
            new("1-korintiers", "1 Korinti\u00ebrs", "1 Corinthians", 46, BibleTestament.New, 16),
            new("openbaring", "Openbaring", "Revelation", 66, BibleTestament.New, 22),
        });
        _sut = new BibleReferenceParser(_books);
    }

    [TestCase("Joh 3:16", "johannes", 3, 16)]
    [TestCase("Joh 3 16", "johannes", 3, 16)]
    [TestCase("Joh 3.16", "johannes", 3, 16)]
    [TestCase("Joh 3 : 16", "johannes", 3, 16)]
    [TestCase("johannes 3:16", "johannes", 3, 16)]
    [TestCase("joh3:16", "johannes", 3, 16)]
    [TestCase("Psalms 23", "psalms", 23, null)]
    [TestCase("Psalms 23 1", "psalms", 23, 1)]
    [TestCase("ps 23:1", "psalms", 23, 1)]
    [TestCase("1joh 4:8", "1-johannes", 4, 8)]
    [TestCase("1 joh 4 8", "1-johannes", 4, 8)]
    [TestCase("1 joh 4", "1-johannes", 4, null)]
    [TestCase("1 kor 13:4", "1-korintiers", 13, 4)]
    [TestCase("openbaring 22:21", "openbaring", 22, 21)]
    [TestCase("op 22", "openbaring", 22, null)]
    public async Task TryParseAsync_RecognisesCommonForms(string input, string expectedBook, int expectedChapter, int? expectedVerse)
    {
        var result = await _sut.TryParseAsync(input);

        result.Should().NotBeNull();
        result!.BookId.Should().Be(expectedBook);
        result.Chapter.Should().Be(expectedChapter);
        result.Verse.Should().Be(expectedVerse);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("xyz")]
    [TestCase("123 :456")]
    public async Task TryParseAsync_RejectsGarbage(string input)
    {
        var result = await _sut.TryParseAsync(input);

        result.Should().BeNull();
    }

    [Test]
    public async Task TryParseAsync_BareBookName_DefaultsChapterToOne()
    {
        var result = await _sut.TryParseAsync("Genesis");

        result.Should().NotBeNull();
        result!.BookId.Should().Be("genesis");
        result.Chapter.Should().Be(1);
        result.Verse.Should().BeNull();
    }
}
