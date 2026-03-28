using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class ChorusCommandServiceTests
{
    private IChorusRepository _repository = null!;
    private IDomainEventDispatcher _eventDispatcher = null!;
    private ILogger<ChorusCommandService> _logger = null!;
    private ChorusCommandService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IChorusRepository>();
        _eventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _logger = Substitute.For<ILogger<ChorusCommandService>>();
        _sut = new ChorusCommandService(_repository, _eventDispatcher, _logger);
    }

    [Test]
    public async Task CreateChorusAsync_WithValidInput_ShouldCreateAndDispatchEvents()
    {
        // Arrange
        var name = "New Chorus";
        var text = "Chorus text";
        var key = MusicalKey.C;
        var type = ChorusType.Praise;
        var timeSignature = TimeSignature.FourFour;

        _repository.AddAsync(Arg.Any<ChorusEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ChorusEntity>());

        // Act
        var result = await _sut.CreateChorusAsync(name, text, key, type, timeSignature);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.ChorusText.Should().Be(text);
        await _repository.Received(1).AddAsync(Arg.Any<ChorusEntity>(), Arg.Any<CancellationToken>());
        await _eventDispatcher.Received(1).DispatchAndClearAsync(Arg.Any<IReadOnlyList<CHAP2.Domain.Events.IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateChorusAsync_WithValidInput_ShouldUpdateChorus()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingChorus = ChorusEntity.Reconstitute(id, "Old Name", "Old Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null);

        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existingChorus);
        _repository.UpdateAsync(Arg.Any<ChorusEntity>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => callInfo.Arg<ChorusEntity>());

        // Act
        var result = await _sut.UpdateChorusAsync(id, "New Name", "New Text", MusicalKey.D, ChorusType.Worship, TimeSignature.ThreeFour);

        // Assert
        result.Name.Should().Be("New Name");
        result.ChorusText.Should().Be("New Text");
        result.Key.Should().Be(MusicalKey.D);
        await _repository.Received(1).UpdateAsync(Arg.Any<ChorusEntity>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateChorusAsync_WhenChorusNotFound_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ChorusEntity?)null);

        // Act
        var act = () => _sut.UpdateChorusAsync(id, "Name", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task DeleteChorusAsync_WhenChorusExists_ShouldDelete()
    {
        // Arrange
        var id = Guid.NewGuid();
        var existingChorus = ChorusEntity.Reconstitute(id, "Name", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null);
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(existingChorus);

        // Act
        await _sut.DeleteChorusAsync(id);

        // Assert
        await _repository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteChorusAsync_WhenChorusNotFound_ShouldThrowException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ChorusEntity?)null);

        // Act
        var act = () => _sut.DeleteChorusAsync(id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().DeleteAsync(id, Arg.Any<CancellationToken>());
    }
}
