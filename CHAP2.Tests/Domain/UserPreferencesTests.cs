using CHAP2.Domain.Entities;
using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;
using FluentAssertions;

namespace CHAP2.Tests.Domain;

[TestFixture]
public class UserPreferencesTests
{
    [Test]
    public void Default_ProducesSystemThemeAndAllScopeAndEnLanguage()
    {
        var prefs = UserPreferences.Default("user-1");

        prefs.UserId.Should().Be("user-1");
        prefs.Theme.Should().Be(Theme.System);
        prefs.DefaultSearchScope.Should().Be(SearchScope.All);
        prefs.Language.Should().Be(Language.En);
        prefs.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void Default_WithEmptyUserId_Throws()
    {
        var act = () => UserPreferences.Default("");
        act.Should().Throw<DomainException>();
    }

    [Test]
    public void Update_AppliesNewValuesAndAdvancesUpdatedAt()
    {
        var prefs = UserPreferences.Default("user-1");
        var initialUpdated = prefs.UpdatedAt;
        Thread.Sleep(5);

        prefs.Update(Theme.Dark, SearchScope.Name, Language.Af);

        prefs.Theme.Should().Be(Theme.Dark);
        prefs.DefaultSearchScope.Should().Be(SearchScope.Name);
        prefs.Language.Should().Be(Language.Af);
        prefs.UpdatedAt.Should().BeAfter(initialUpdated);
    }
}
