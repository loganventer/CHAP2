using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using CHAP2.Domain.Events;
using FluentAssertions;

namespace CHAP2.Tests.Domain;

[TestFixture]
public class ChorusTests
{
    [Test]
    public void Create_WithValidParameters_ShouldCreateChorus()
    {
        // Arrange
        var name = "Test Chorus";
        var chorusText = "This is the chorus text";
        var key = MusicalKey.C;
        var type = ChorusType.Praise;
        var timeSignature = TimeSignature.FourFour;

        // Act
        var chorus = ChorusEntity.Create(name, chorusText, key, type, timeSignature);

        // Assert
        chorus.Should().NotBeNull();
        chorus.Id.Should().NotBeEmpty();
        chorus.Name.Should().Be(name);
        chorus.ChorusText.Should().Be(chorusText);
        chorus.Key.Should().Be(key);
        chorus.Type.Should().Be(type);
        chorus.TimeSignature.Should().Be(timeSignature);
        chorus.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        chorus.UpdatedAt.Should().BeNull();
    }

    [Test]
    public void Create_ShouldRaiseChorusCreatedEvent()
    {
        // Act
        var chorus = ChorusEntity.Create("Test", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Assert
        chorus.DomainEvents.Should().ContainSingle();
        chorus.DomainEvents.First().Should().BeOfType<ChorusCreatedEvent>();
        var createdEvent = (ChorusCreatedEvent)chorus.DomainEvents.First();
        createdEvent.ChorusId.Should().Be(chorus.Id);
        createdEvent.ChorusName.Should().Be(chorus.Name);
    }

    [Test]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        // Act
        var act = () => ChorusEntity.Create("", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Test]
    public void Create_WithEmptyChorusText_ShouldThrowDomainException()
    {
        // Act
        var act = () => ChorusEntity.Create("Name", "", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*text*");
    }

    [Test]
    public void Create_WithNotSetKey_ShouldThrowDomainException()
    {
        // Act
        var act = () => ChorusEntity.Create("Name", "Text", MusicalKey.NotSet, ChorusType.Praise, TimeSignature.FourFour);

        // Assert
        act.Should().Throw<DomainException>().WithMessage("*key*");
    }

    [Test]
    public void CreateFromSlide_WithValidParameters_ShouldCreateChorusWithNotSetValues()
    {
        // Arrange
        var name = "Slide Chorus";
        var chorusText = "Slide text content";

        // Act
        var chorus = ChorusEntity.CreateFromSlide(name, chorusText);

        // Assert
        chorus.Should().NotBeNull();
        chorus.Name.Should().Be(name);
        chorus.ChorusText.Should().Be(chorusText);
        chorus.Key.Should().Be(MusicalKey.NotSet);
        chorus.Type.Should().Be(ChorusType.NotSet);
        chorus.TimeSignature.Should().Be(TimeSignature.NotSet);
    }

    [Test]
    public void Update_WithValidParameters_ShouldUpdateChorusAndRaiseEvent()
    {
        // Arrange
        var chorus = ChorusEntity.Create("Original", "Original text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);
        chorus.ClearDomainEvents();

        // Act
        chorus.Update("Updated", "Updated text", MusicalKey.D, ChorusType.Worship, TimeSignature.ThreeFour);

        // Assert
        chorus.Name.Should().Be("Updated");
        chorus.ChorusText.Should().Be("Updated text");
        chorus.Key.Should().Be(MusicalKey.D);
        chorus.Type.Should().Be(ChorusType.Worship);
        chorus.TimeSignature.Should().Be(TimeSignature.ThreeFour);
        chorus.UpdatedAt.Should().NotBeNull();
        chorus.DomainEvents.Should().ContainSingle();
        chorus.DomainEvents.First().Should().BeOfType<ChorusUpdatedEvent>();
    }

    [Test]
    public void Reconstitute_ShouldCreateChorusWithoutRaisingEvents()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var updatedAt = DateTime.UtcNow;

        // Act
        var chorus = ChorusEntity.Reconstitute(
            id, "Name", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour,
            createdAt, updatedAt, null);

        // Assert
        chorus.Id.Should().Be(id);
        chorus.CreatedAt.Should().Be(createdAt);
        chorus.UpdatedAt.Should().Be(updatedAt);
        chorus.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void ContainsSearchTerm_WithMatchingName_ShouldReturnTrue()
    {
        // Arrange
        var chorus = ChorusEntity.Create("Amazing Grace", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Act & Assert
        chorus.ContainsSearchTerm("amazing").Should().BeTrue();
        chorus.ContainsSearchTerm("grace").Should().BeTrue();
        chorus.ContainsSearchTerm("GRACE").Should().BeTrue();
    }

    [Test]
    public void ContainsSearchTerm_WithNoMatch_ShouldReturnFalse()
    {
        // Arrange
        var chorus = ChorusEntity.Create("Amazing Grace", "Text", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour);

        // Act & Assert
        chorus.ContainsSearchTerm("xyz").Should().BeFalse();
    }

    [Test]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var chorus1 = ChorusEntity.Reconstitute(id, "Name1", "Text1", MusicalKey.C, ChorusType.Praise, TimeSignature.FourFour, DateTime.UtcNow, null, null);
        var chorus2 = ChorusEntity.Reconstitute(id, "Name2", "Text2", MusicalKey.D, ChorusType.Worship, TimeSignature.ThreeFour, DateTime.UtcNow, null, null);

        // Act & Assert
        chorus1.Should().Be(chorus2);
        (chorus1 == chorus2).Should().BeTrue();
    }
}
