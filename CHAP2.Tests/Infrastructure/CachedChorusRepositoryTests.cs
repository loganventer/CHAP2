using CHAP2.Application.Interfaces;
using CHAP2.Domain.Enums;
using CHAP2.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Infrastructure;

[TestFixture]
public class CachedChorusRepositoryTests
{
    private IChorusRepository _innerRepository = null!;
    private IMemoryCache _cache = null!;
    private ILogger<CachedChorusRepository> _logger = null!;
    private CachedChorusRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _innerRepository = Substitute.For<IChorusRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<CachedChorusRepository>>();
        _sut = new CachedChorusRepository(_innerRepository, _cache, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    private static ChorusEntity CreateTestChorus(Guid? id = null, string name = "Test Chorus")
    {
        return ChorusEntity.Reconstitute(
            id ?? Guid.NewGuid(), name, "Text", MusicalKey.C, ChorusType.Praise,
            TimeSignature.FourFour, DateTime.UtcNow, null, null);
    }

    // --- Cache Hit ---

    [Test]
    public async Task GetByIdAsync_CacheHit_ShouldReturnCachedDataWithoutCallingInner()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus = CreateTestChorus(id);
        // First call populates cache
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);
        await _sut.GetByIdAsync(id);

        // Reset the inner substitute to track the second call
        _innerRepository.ClearReceivedCalls();

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        await _innerRepository.DidNotReceive().GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAllAsync_CacheHit_ShouldReturnCachedDataWithoutCallingInner()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus() };
        _innerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(choruses);
        await _sut.GetAllAsync();
        _innerRepository.ClearReceivedCalls();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(1);
        await _innerRepository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
    }

    // --- Cache Miss ---

    [Test]
    public async Task GetByIdAsync_CacheMiss_ShouldDelegateToInnerRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus = CreateTestChorus(id);
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        await _innerRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAllAsync_CacheMiss_ShouldDelegateToInnerRepository()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus(), CreateTestChorus(name: "Second") };
        _innerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        await _innerRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetByNameAsync_CacheMiss_ShouldDelegateToInnerRepository()
    {
        // Arrange
        var name = "Test Chorus";
        var chorus = CreateTestChorus(name: name);
        _innerRepository.GetByNameAsync(name, Arg.Any<CancellationToken>()).Returns(chorus);

        // Act
        var result = await _sut.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(name);
        await _innerRepository.Received(1).GetByNameAsync(name, Arg.Any<CancellationToken>());
    }

    // --- Write Operations Invalidate Cache ---

    [Test]
    public async Task AddAsync_ShouldInvalidateCacheAndDelegateToInner()
    {
        // Arrange - populate cache
        var choruses = new List<ChorusEntity> { CreateTestChorus() };
        _innerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(choruses);
        await _sut.GetAllAsync(); // populate cache

        var newChorus = CreateTestChorus(name: "New Chorus");
        _innerRepository.AddAsync(newChorus, Arg.Any<CancellationToken>()).Returns(newChorus);

        // Act
        await _sut.AddAsync(newChorus);

        // Assert
        await _innerRepository.Received(1).AddAsync(newChorus, Arg.Any<CancellationToken>());

        // Verify cache was invalidated - next GetAllAsync should call inner
        _innerRepository.ClearReceivedCalls();
        var updatedChoruses = new List<ChorusEntity> { CreateTestChorus(), newChorus };
        _innerRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(updatedChoruses);
        await _sut.GetAllAsync();
        await _innerRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateAsync_ShouldInvalidateCacheAndDelegateToInner()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus = CreateTestChorus(id);
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);
        await _sut.GetByIdAsync(id); // populate cache

        _innerRepository.UpdateAsync(chorus, Arg.Any<CancellationToken>()).Returns(chorus);

        // Act
        await _sut.UpdateAsync(chorus);

        // Assert
        await _innerRepository.Received(1).UpdateAsync(chorus, Arg.Any<CancellationToken>());

        // Verify cache was invalidated - next GetByIdAsync should call inner
        _innerRepository.ClearReceivedCalls();
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);
        await _sut.GetByIdAsync(id);
        await _innerRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteAsync_ShouldInvalidateCacheAndDelegateToInner()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus = CreateTestChorus(id);
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(chorus);
        await _sut.GetByIdAsync(id); // populate cache

        // Act
        await _sut.DeleteAsync(id);

        // Assert
        await _innerRepository.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());

        // Verify cache was invalidated
        _innerRepository.ClearReceivedCalls();
        _innerRepository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ChorusEntity?)null);
        await _sut.GetByIdAsync(id);
        await _innerRepository.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    // --- SearchAsync delegates without caching ---

    [Test]
    public async Task SearchAsync_ShouldAlwaysDelegateToInnerRepository()
    {
        // Arrange
        var choruses = new List<ChorusEntity> { CreateTestChorus() };
        _innerRepository.SearchAsync("test", Arg.Any<CancellationToken>()).Returns(choruses);

        // Act
        var result = await _sut.SearchAsync("test");

        // Assert
        result.Should().HaveCount(1);
        await _innerRepository.Received(1).SearchAsync("test", Arg.Any<CancellationToken>());
    }
}
