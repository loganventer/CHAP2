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

    [Test]
    public async Task SearchVersesAsync_ExactPhraseAppearsAtTop()
    {
        var sut = CreateSut();

        // "een geloof" appears verbatim in Efesi\u00ebrs 4:5 ("...een geloof een doop...").
        var results = await sut.SearchVersesAsync("een geloof", 10);

        results.Should().NotBeEmpty();
        results[0].BookId.Should().Be("efesiers");
        results[0].Chapter.Should().Be(4);
        results[0].Verse.Should().Be(5);
    }

    [Test]
    public async Task SearchVersesAsync_QueryWithDiacriticsMatchesNormalizedText()
    {
        var sut = CreateSut();

        // The user is unlikely to type the diacritic; ASCII fallback should
        // still match w\u00ebreld in Joh 3:16.
        var ascii = await sut.SearchVersesAsync("wereld gehad", 5);

        ascii.Should().NotBeEmpty();
        ascii.Should().Contain(v => v.BookId == "johannes" && v.Chapter == 3 && v.Verse == 16);
    }

    [Test]
    public async Task SearchVersesAsync_ResultsLimitedToMax()
    {
        var sut = CreateSut();

        // "God" is a very common word; cap at 7 -- the result should be exactly 7
        // and reflect the top-ranked hits, not the first seven by chapter order.
        var results = await sut.SearchVersesAsync("God", 7);

        results.Should().HaveCount(7);
    }

    [Test]
    public async Task SearchVersesAsync_ExactPhraseOutranksJumbledOccurrences()
    {
        var sut = CreateSut();

        // "in die begin" is an exact phrase in many verses. Top should be
        // the SHORTEST one carrying the phrase (Joh 1:2 -- "Hy was in die
        // begin by God."), proving:
        //   - exact-phrase tier is winning over out-of-order (score)
        //   - the verse-length tiebreaker is being applied within the tier
        var results = await sut.SearchVersesAsync("in die begin", 5);

        results.Should().NotBeEmpty();
        results[0].BookId.Should().Be("johannes");
        results[0].Chapter.Should().Be(1);
        results[0].Verse.Should().Be(2);
        // Genesis 1:1 also matches the exact phrase, just longer; it
        // should be in the top 5 but after Joh 1:2.
        results.Should().Contain(v => v.BookId == "genesis" && v.Chapter == 1 && v.Verse == 1);
    }

    [Test]
    public async Task SearchVersesAsync_GarbageQueryReturnsEmpty()
    {
        var sut = CreateSut();

        var results = await sut.SearchVersesAsync("xqzwblahnotpresent", 5);

        results.Should().BeEmpty();
    }
}
