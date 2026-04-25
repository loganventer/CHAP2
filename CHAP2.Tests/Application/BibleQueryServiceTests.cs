using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class BibleQueryServiceTests
{
    private IBibleRepository _repo = null!;
    private IBibleReferenceParser _parser = null!;
    private ILogger<BibleQueryService> _logger = null!;
    private BibleQueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IBibleRepository>();
        _parser = Substitute.For<IBibleReferenceParser>();
        _logger = Substitute.For<ILogger<BibleQueryService>>();
        _sut = new BibleQueryService(_repo, _parser, _logger);
    }

    [Test]
    public async Task GetBooksAsync_DelegatesToRepository()
    {
        var books = new List<BibleBook>
        {
            new("genesis", "Genesis", "Genesis", 1, BibleTestament.Old, 50),
        };
        _repo.GetAllBooksAsync(Arg.Any<CancellationToken>()).Returns(books);

        var result = await _sut.GetBooksAsync();

        result.Should().BeEquivalentTo(books);
    }

    [Test]
    public async Task GetChapterAsync_WithBlankBookId_ReturnsNull()
    {
        var result = await _sut.GetChapterAsync(" ", 1);

        result.Should().BeNull();
        await _repo.DidNotReceive().GetChapterAsync(Arg.Any<BibleReference>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetChapterAsync_WithChapterBelowOne_ReturnsNull()
    {
        var result = await _sut.GetChapterAsync("genesis", 0);

        result.Should().BeNull();
        await _repo.DidNotReceive().GetChapterAsync(Arg.Any<BibleReference>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetChapterAsync_PassesBuiltReferenceToRepository()
    {
        var book = new BibleBook("genesis", "Genesis", "Genesis", 1, BibleTestament.Old, 50);
        var verse = new BibleVerse("genesis", "Genesis", 1, 1, "In die begin ...");
        var chapter = new BibleChapter(book, 1, new[] { verse });
        _repo.GetChapterAsync(Arg.Is<BibleReference>(r => r.BookId == "genesis" && r.Chapter == 3), Arg.Any<CancellationToken>())
            .Returns(chapter);

        var result = await _sut.GetChapterAsync("genesis", 3);

        result.Should().Be(chapter);
    }

    [Test]
    public async Task SearchAsync_WithBlankQuery_ReturnsEmpty()
    {
        var result = await _sut.SearchAsync("  ", 100);

        result.Should().BeEmpty();
        await _repo.DidNotReceive().SearchVersesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchAsync_NonPositiveMax_ClampsToDefault()
    {
        var verses = new List<BibleVerse>
        {
            new("johannes", "Johannes", 3, 16, "Want so lief het God ..."),
        };
        _repo.SearchVersesAsync("liefde", 100, Arg.Any<CancellationToken>()).Returns(verses);

        var result = await _sut.SearchAsync("liefde", 0);

        result.Should().HaveCount(1);
        await _repo.Received(1).SearchVersesAsync("liefde", 100, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchAsync_TooLargeMax_ClampsToHardCap()
    {
        _repo.SearchVersesAsync(Arg.Any<string>(), 500, Arg.Any<CancellationToken>())
            .Returns(new List<BibleVerse>());

        await _sut.SearchAsync("liefde", 99999);

        await _repo.Received(1).SearchVersesAsync("liefde", 500, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ResolveReferenceAsync_DelegatesToParser()
    {
        var expected = new BibleReference("johannes", 3, 16);
        _parser.TryParseAsync("Joh 3:16", Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.ResolveReferenceAsync("Joh 3:16");

        result.Should().Be(expected);
    }

    [Test]
    public async Task ResolveReferenceAsync_BlankInput_ReturnsNullWithoutCallingParser()
    {
        var result = await _sut.ResolveReferenceAsync("  ");

        result.Should().BeNull();
        await _parser.DidNotReceive().TryParseAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
