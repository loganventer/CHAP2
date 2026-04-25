using CHAP2.Domain.ValueObjects;
using CHAP2.Infrastructure.Repositories.Bible;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CHAP2.Tests.Infrastructure;

[TestFixture]
public class DiskBibleRepositoryTests
{
    private static string LocateAovRoot()
    {
        // Walk up from the test bin folder until we find data/bible/aov.
        var dir = TestContext.CurrentContext.TestDirectory;
        for (var depth = 0; depth < 8 && dir is not null; depth++, dir = Path.GetDirectoryName(dir))
        {
            var candidate = Path.Combine(dir, "data", "bible", "aov");
            if (File.Exists(Path.Combine(candidate, "_books.json")))
                return candidate;
        }
        Assert.Ignore("AOV data not found alongside the test directory; skipping integration tests.");
        return string.Empty;
    }

    private DiskBibleRepository CreateSut() =>
        new(LocateAovRoot(), NullLogger<DiskBibleRepository>.Instance);

    [Test]
    public async Task GetAllBooksAsync_Returns66Books()
    {
        var sut = CreateSut();

        var books = await sut.GetAllBooksAsync();

        books.Should().HaveCount(66);
        books.Select(b => b.Ordinal).Should().BeInAscendingOrder();
    }

    [Test]
    public async Task GetChapterAsync_LoadsPsalm23WithExpectedFirstVerse()
    {
        var sut = CreateSut();

        var chapter = await sut.GetChapterAsync(new BibleReference("psalms", 23));

        chapter.Should().NotBeNull();
        chapter!.Number.Should().Be(23);
        chapter.Verses.Should().HaveCount(6);
        chapter.Verses[0].Text.Should().Contain("HERE is my herder");
    }

    [Test]
    public async Task GetChapterAsync_LoadsJohannes3WithExpectedVerse16()
    {
        var sut = CreateSut();

        var chapter = await sut.GetChapterAsync(new BibleReference("johannes", 3, 16));

        chapter.Should().NotBeNull();
        chapter!.Verses.Single(v => v.Verse == 16).Text.Should().Contain("Want so lief het God die w\u00eareld gehad");
    }

    [Test]
    public async Task GetChapterAsync_OutOfRangeChapter_ReturnsNull()
    {
        var sut = CreateSut();

        var chapter = await sut.GetChapterAsync(new BibleReference("genesis", 999));

        chapter.Should().BeNull();
    }

    [Test]
    public async Task SearchVersesAsync_FindsCommonAfrikaansWord()
    {
        var sut = CreateSut();

        var results = await sut.SearchVersesAsync("liefde", 5);

        results.Should().NotBeEmpty();
        results.Should().HaveCountLessThanOrEqualTo(5);
    }
}
