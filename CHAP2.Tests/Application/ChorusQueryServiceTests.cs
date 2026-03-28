using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class ChorusQueryServiceTests
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

    [Test]
    public async Task GetChorusByIdAsync_WhenChorusExists_ShouldReturnChorus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedChorus = ChorusEntity.Reconstitute(id, "Test", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(expectedChorus);

        // Act
        var result = await _sut.GetChorusByIdAsync(id);

        // Assert
        result.Should().Be(expectedChorus);
        await _repository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetChorusByIdAsync_WhenChorusNotFound_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ChorusEntity?)null);

        // Act
        var result = await _sut.GetChorusByIdAsync(id);

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public async Task GetAllChorusesAsync_ShouldReturnAllChoruses()
    {
        // Arrange
        var choruses = new List<ChorusEntity>
        {
            ChorusEntity.Reconstitute(Guid.NewGuid(), "Test1", "Text1", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null),
            ChorusEntity.Reconstitute(Guid.NewGuid(), "Test2", "Text2", MusicalKey.D, ChorusType.Worship, TimeSignature.ThreeFour, DateTime.UtcNow, null, null)
        };
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.GetAllChorusesAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(choruses);
    }

    [Test]
    public async Task GetChorusByNameAsync_WhenChorusExists_ShouldReturnChorus()
    {
        // Arrange
        var name = "Test Chorus";
        var expectedChorus = ChorusEntity.Reconstitute(Guid.NewGuid(), name, "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null);
        _repository.GetByNameAsync(name, Arg.Any<CancellationToken>()).Returns(expectedChorus);

        // Act
        var result = await _sut.GetChorusByNameAsync(name);

        // Assert
        result.Should().Be(expectedChorus);
        await _repository.Received(1).GetByNameAsync(name, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchChorusesAsync_ShouldDelegateToRepository()
    {
        // Arrange
        var searchTerm = "test";
        var choruses = new List<ChorusEntity>
        {
            ChorusEntity.Reconstitute(Guid.NewGuid(), "Test Chorus", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null)
        };
        _repository.SearchAsync(searchTerm, Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchChorusesAsync(searchTerm, SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().HaveCount(1);
        await _repository.Received(1).SearchAsync(searchTerm, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SearchChorusesAsync_WithEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var searchTerm = "";

        // Act
        var result = await _sut.SearchChorusesAsync(searchTerm, SearchMode.Contains, SearchScope.All);

        // Assert
        result.Should().BeEmpty();
        await _repository.DidNotReceive().SearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
