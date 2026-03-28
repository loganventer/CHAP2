using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class ChorusSearchServiceTests
{
    private IChorusRepository _repository = null!;
    private ILogger<ChorusQueryService> _logger = null!;
    private ChorusQueryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IChorusRepository>();
        _logger = Substitute.For<ILogger<ChorusQueryService>>();
        _sut = new ChorusQueryService(_repository, _logger);
    }

    private static ChorusEntity CreateTestChorus(string name = "Test", string text = "Text", MusicalKey key = MusicalKey.C)
    {
        return ChorusEntity.Reconstitute(
            Guid.NewGuid(), name, text, key, ChorusType.Praise,
            TimeSignature.FourFour, DateTime.UtcNow, null, null);
    }

    // --- SearchByName with Contains mode ---

    [Test]
    public async Task SearchChorusesAsync_ContainsMode_ShouldDelegateToRepository()
    {
        // Arrange
        var choruses = new List<ChorusEntity>
        {
            CreateTestChorus("Amazing Grace"),
            CreateTestChorus("Grace Alone")
        };
        _repository.SearchAsync("grace", Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchChorusesAsync("grace", SearchMode.Contains, SearchScope.Name);

        // Assert
        result.Should().HaveCount(2);
        await _repository.Received(1).SearchAsync("grace", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchChorusesAsync_ExactMode_ShouldDelegateToRepository()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus("Amazing Grace") };
        _repository.SearchAsync("Amazing Grace", Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchChorusesAsync("Amazing Grace", SearchMode.Exact, SearchScope.Name);

        // Assert
        result.Should().HaveCount(1);
        await _repository.Received(1).SearchAsync("Amazing Grace", Arg.Any<CancellationToken>());
    }

    // --- SearchByText ---

    [Test]
    public async Task SearchChorusesAsync_TextScope_ShouldDelegateToRepository()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus("Chorus One", "praise the Lord") };
        _repository.SearchAsync("praise", Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchChorusesAsync("praise", SearchMode.Contains, SearchScope.Text);

        // Assert
        result.Should().HaveCount(1);
        await _repository.Received(1).SearchAsync("praise", Arg.Any<CancellationToken>());
    }

    // --- SearchAll deduplication ---

    [Test]
    public async Task SearchChorusesAsync_AllScope_ShouldReturnResults()
    {
        // Arrange
        var chorus = CreateTestChorus("Amazing Grace", "How sweet the sound");
        var choruses = new List<ChorusEntity> { chorus };
        _repository.SearchAsync("amazing", Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchChorusesAsync("amazing", SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Amazing Grace");
    }

    // --- Empty/null query handling ---

    [Test]
    public async Task SearchChorusesAsync_EmptyQuery_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.SearchChorusesAsync("", SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().BeEmpty();
        await _repository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchChorusesAsync_NullQuery_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.SearchChorusesAsync(null!, SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().BeEmpty();
        await _repository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchChorusesAsync_WhitespaceQuery_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.SearchChorusesAsync("   ", SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().BeEmpty();
        await _repository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
