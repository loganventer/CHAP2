using CHAP2.Chorus.Api.HostedServices;
using FluentAssertions;

namespace CHAP2.Tests.Api;

[TestFixture]
public class ChorusGitSyncBackgroundServiceTests
{
    [Test]
    public void NextDelayUntil_FireTimeLaterToday_ReturnsHoursUntilThen()
    {
        var now = new DateTime(2026, 04, 27, 09, 30, 00, DateTimeKind.Utc);
        var fireAt = new TimeOnly(13, 00);

        var delay = ChorusGitSyncBackgroundService.NextDelayUntil(fireAt, now);

        delay.Should().Be(TimeSpan.FromMinutes(3 * 60 + 30));
    }

    [Test]
    public void NextDelayUntil_FireTimeAlreadyPassedToday_RollsOverToTomorrow()
    {
        var now = new DateTime(2026, 04, 27, 18, 00, 00, DateTimeKind.Utc);
        var fireAt = new TimeOnly(03, 00);

        var delay = ChorusGitSyncBackgroundService.NextDelayUntil(fireAt, now);

        // 03:00 the next day = 9 hours from 18:00.
        delay.Should().Be(TimeSpan.FromHours(9));
    }

    [Test]
    public void NextDelayUntil_FireTimeIsNow_RollsOverToTomorrow()
    {
        var now = new DateTime(2026, 04, 27, 03, 00, 00, DateTimeKind.Utc);
        var fireAt = new TimeOnly(03, 00);

        var delay = ChorusGitSyncBackgroundService.NextDelayUntil(fireAt, now);

        delay.Should().Be(TimeSpan.FromHours(24));
    }
}
