using CHAP2.Application.Interfaces;
using CHAP2.Application.Services;
using CHAP2.Domain.Entities;
using CHAP2.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace CHAP2.Tests.Application;

[TestFixture]
public class SetlistOwnershipPolicyTests
{
    [Test]
    public void EnsureCanAccess_OwnerIsAllowed()
    {
        var current = Substitute.For<ICurrentUserService>();
        current.UserId.Returns("user-1");
        current.IsAdmin.Returns(false);
        var policy = new SetlistOwnershipPolicy(current);
        var setlist = Setlist.Create("user-1", "Service");

        var act = () => policy.EnsureCanAccess(setlist);

        act.Should().NotThrow();
    }

    [Test]
    public void EnsureCanAccess_AdminIsAllowed_EvenForOtherOwner()
    {
        var current = Substitute.For<ICurrentUserService>();
        current.UserId.Returns("admin-id");
        current.IsAdmin.Returns(true);
        var policy = new SetlistOwnershipPolicy(current);
        var setlist = Setlist.Create("someone-else", "Service");

        var act = () => policy.EnsureCanAccess(setlist);

        act.Should().NotThrow();
    }

    [Test]
    public void EnsureCanAccess_NonOwnerNonAdminThrows()
    {
        var current = Substitute.For<ICurrentUserService>();
        current.UserId.Returns("intruder");
        current.IsAdmin.Returns(false);
        var policy = new SetlistOwnershipPolicy(current);
        var setlist = Setlist.Create("owner", "Service");

        var act = () => policy.EnsureCanAccess(setlist);

        act.Should().Throw<SetlistAccessDeniedException>();
    }
}
