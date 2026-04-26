using CHAP2.Application.Helpers;
using FluentAssertions;

namespace CHAP2.Tests.Application;

[TestFixture]
public class SecretMaskTests
{
    [TestCase(
        "git pull failed: https://x-access-token:ghp_AbCdE1234@github.com/loganventer/CHAP2.git",
        "git pull failed: https://x-access-token:***@github.com/loganventer/CHAP2.git")]
    [TestCase(
        "fatal: Authentication failed for 'https://user:secret@host/path'",
        "fatal: Authentication failed for 'https://user:***@host/path'")]
    [TestCase(
        "no credentials in this string",
        "no credentials in this string")]
    [TestCase("", "")]
    public void Apply_MasksInlineCredentials(string input, string expected)
    {
        SecretMask.Apply(input).Should().Be(expected);
    }

    [Test]
    public void Apply_NullSafe()
    {
        SecretMask.Apply(null).Should().BeEmpty();
    }
}
