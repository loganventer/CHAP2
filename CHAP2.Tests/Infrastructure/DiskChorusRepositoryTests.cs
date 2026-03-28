using CHAP2.Domain.Enums;
using CHAP2.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CHAP2.Tests.Infrastructure;

[TestFixture]
public class DiskChorusRepositoryTests
{
    private string _tempDir = null!;
    private ILogger<DiskChorusRepository> _logger = null!;
    private DiskChorusRepository _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "CHAP2Tests", Guid.NewGuid().ToString());
        _logger = Substitute.For<ILogger<DiskChorusRepository>>();
        _sut = new DiskChorusRepository(_tempDir, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    private ChorusEntity CreateTestChorus(string name = "Test Chorus", string text = "Test text")
    {
        return ChorusEntity.Create(name, text, MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);
    }

    // --- GetByIdAsync ---

    [Test]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnChorus()
    {
        // Arrange
        var chorus = CreateTestChorus();
        await _sut.AddAsync(chorus);

        // Act
        var result = await _sut.GetByIdAsync(chorus.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(chorus.Id);
        result.Name.Should().Be(chorus.Name);
        result.ChorusText.Should().Be(chorus.ChorusText);
    }

    [Test]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // --- GetAllAsync ---

    [Test]
    public async Task GetAllAsync_WithEmptyDirectory_ShouldReturnEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAllAsync_WithPopulatedDirectory_ShouldReturnAllChoruses()
    {
        // Arrange
        var chorus1 = CreateTestChorus("Chorus One", "Text one");
        var chorus2 = CreateTestChorus("Chorus Two", "Text two");
        await _sut.AddAsync(chorus1);
        await _sut.AddAsync(chorus2);

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Select(c => c.Name).Should().Contain("Chorus One").And.Contain("Chorus Two");
    }

    // --- AddAsync ---

    [Test]
    public async Task AddAsync_WithValidChorus_ShouldWriteJsonFile()
    {
        // Arrange
        var chorus = CreateTestChorus("Amazing Grace", "Amazing grace how sweet the sound");

        // Act
        var result = await _sut.AddAsync(chorus);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(chorus.Id);
        var filePath = Path.Combine(_tempDir, $"{chorus.Id}.json");
        File.Exists(filePath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(filePath);
        json.Should().Contain("Amazing Grace");
        json.Should().Contain("Amazing grace how sweet the sound");
    }

    // --- UpdateAsync ---

    [Test]
    public async Task UpdateAsync_WithExistingChorus_ShouldOverwriteFile()
    {
        // Arrange
        var chorus = CreateTestChorus("Original Name", "Original text");
        await _sut.AddAsync(chorus);
        chorus.Update("Updated Name", "Updated text", MusicalKey.D, ChorusType.Worship, TimeSignature.ThreeFour);

        // Act
        var result = await _sut.UpdateAsync(chorus);

        // Assert
        result.Should().NotBeNull();
        var retrieved = await _sut.GetByIdAsync(chorus.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Name");
        retrieved.ChorusText.Should().Be("Updated text");
        retrieved.Key.Should().Be(MusicalKey.D);
    }

    // --- DeleteAsync ---

    [Test]
    public async Task DeleteAsync_WithExistingId_ShouldRemoveFile()
    {
        // Arrange
        var chorus = CreateTestChorus();
        await _sut.AddAsync(chorus);
        var filePath = Path.Combine(_tempDir, $"{chorus.Id}.json");
        File.Exists(filePath).Should().BeTrue();

        // Act
        await _sut.DeleteAsync(chorus.Id);

        // Assert
        File.Exists(filePath).Should().BeFalse();
        var result = await _sut.GetByIdAsync(chorus.Id);
        result.Should().BeNull();
    }

    [Test]
    public async Task DeleteAsync_WithNonExistentId_ShouldNotThrow()
    {
        // Act
        var act = () => _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        await act.Should().NotThrowAsync();
    }

    // --- SearchAsync ---

    [Test]
    public async Task SearchAsync_WithMatchingName_ShouldReturnResults()
    {
        // Arrange
        var chorus1 = CreateTestChorus("Amazing Grace", "How sweet the sound");
        var chorus2 = CreateTestChorus("Holy Holy", "Holy is the Lord");
        await _sut.AddAsync(chorus1);
        await _sut.AddAsync(chorus2);

        // Act
        var results = await _sut.SearchAsync("amazing");

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Amazing Grace");
    }

    [Test]
    public async Task SearchAsync_WithMatchingText_ShouldReturnResults()
    {
        // Arrange
        var chorus1 = CreateTestChorus("Chorus One", "Praise the Lord always");
        var chorus2 = CreateTestChorus("Chorus Two", "Something else entirely");
        await _sut.AddAsync(chorus1);
        await _sut.AddAsync(chorus2);

        // Act
        var results = await _sut.SearchAsync("praise");

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Chorus One");
    }

    [Test]
    public async Task SearchAsync_WithNoMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var chorus = CreateTestChorus("Test Chorus", "Some text");
        await _sut.AddAsync(chorus);

        // Act
        var results = await _sut.SearchAsync("nonexistent");

        // Assert
        results.Should().BeEmpty();
    }

    [Test]
    public void SearchAsync_WithEmptySearchTerm_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _sut.SearchAsync("");

        // Assert
        act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task SearchAsync_WithCaseInsensitiveTerm_ShouldReturnResults()
    {
        // Arrange
        var chorus = CreateTestChorus("Amazing Grace", "How sweet the sound");
        await _sut.AddAsync(chorus);

        // Act
        var results = await _sut.SearchAsync("AMAZING");

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Amazing Grace");
    }
}
