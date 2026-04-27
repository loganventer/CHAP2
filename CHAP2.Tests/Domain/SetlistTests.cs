using CHAP2.Domain.Entities;
using CHAP2.Domain.Events;
using CHAP2.Domain.Exceptions;
using FluentAssertions;

namespace CHAP2.Tests.Domain;

[TestFixture]
public class SetlistTests
{
    [Test]
    public void Create_WithValidArguments_AssignsValuesAndRaisesCreatedEvent()
    {
        var setlist = Setlist.Create("user-1", "Sunday morning");

        setlist.Id.Should().NotBeEmpty();
        setlist.OwnerId.Should().Be("user-1");
        setlist.Name.Should().Be("Sunday morning");
        setlist.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        setlist.UpdatedAt.Should().BeNull();
        setlist.Items.Should().BeEmpty();
        setlist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SetlistCreatedEvent>();
    }

    [Test]
    public void Create_WithEmptyOwner_Throws()
    {
        var act = () => Setlist.Create("", "Name");
        act.Should().Throw<DomainException>().WithMessage("*Owner*");
    }

    [Test]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => Setlist.Create("user-1", "");
        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Test]
    public void Rename_UpdatesNameAndRaisesUpdatedEvent()
    {
        var setlist = Setlist.Create("user-1", "Original");
        setlist.ClearDomainEvents();

        setlist.Rename("Renamed");

        setlist.Name.Should().Be("Renamed");
        setlist.UpdatedAt.Should().NotBeNull();
        setlist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SetlistUpdatedEvent>();
    }

    [Test]
    public void AppendChorus_AppendsAtEndAndAssignsZeroBasedPosition()
    {
        var setlist = Setlist.Create("user-1", "Service");

        var first = setlist.AppendChorus(Guid.NewGuid());
        var second = setlist.AppendChorus(Guid.NewGuid());

        setlist.Items.Should().HaveCount(2);
        first.Position.Should().Be(0);
        second.Position.Should().Be(1);
    }

    [Test]
    public void RemoveItem_RecompactsRemainingPositions()
    {
        var setlist = Setlist.Create("user-1", "Service");
        var a = setlist.AppendChorus(Guid.NewGuid());
        var b = setlist.AppendChorus(Guid.NewGuid());
        var c = setlist.AppendChorus(Guid.NewGuid());

        setlist.RemoveItem(b.Id);

        setlist.Items.Select(i => i.Id).Should().Equal(a.Id, c.Id);
        setlist.Items[0].Position.Should().Be(0);
        setlist.Items[1].Position.Should().Be(1);
    }

    [Test]
    public void Reorder_AppliesNewOrderToPositions()
    {
        var setlist = Setlist.Create("user-1", "Service");
        var a = setlist.AppendChorus(Guid.NewGuid());
        var b = setlist.AppendChorus(Guid.NewGuid());
        var c = setlist.AppendChorus(Guid.NewGuid());

        setlist.Reorder(new[] { c.Id, a.Id, b.Id });

        setlist.Items.Select(i => i.Id).Should().Equal(c.Id, a.Id, b.Id);
        setlist.Items.Select(i => i.Position).Should().Equal(0, 1, 2);
    }

    [Test]
    public void Reorder_WithMissingOrExtraIds_Throws()
    {
        var setlist = Setlist.Create("user-1", "Service");
        var a = setlist.AppendChorus(Guid.NewGuid());

        var act = () => setlist.Reorder(new[] { a.Id, Guid.NewGuid() });

        act.Should().Throw<DomainException>();
    }

    [Test]
    public void MarkDeleted_RaisesDeletedEvent()
    {
        var setlist = Setlist.Create("user-1", "Service");
        setlist.ClearDomainEvents();

        setlist.MarkDeleted();

        setlist.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<SetlistDeletedEvent>();
    }
}
