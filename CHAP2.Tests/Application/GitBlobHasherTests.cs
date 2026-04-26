using System.Text;
using CHAP2.Application.Helpers;
using FluentAssertions;

namespace CHAP2.Tests.Application;

[TestFixture]
public class GitBlobHasherTests
{
    [Test]
    public void Compute_MatchesGitsHashOfEmptyBlob()
    {
        // `git hash-object` of an empty file is this exact SHA.
        GitBlobHasher.Compute(Array.Empty<byte>())
            .Should().Be("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391");
    }

    [Test]
    public void Compute_MatchesGitsHashOfHelloWorld()
    {
        // `printf 'hello world' | git hash-object --stdin` -> 95d09f2b...
        GitBlobHasher.Compute(Encoding.UTF8.GetBytes("hello world"))
            .Should().Be("95d09f2b10159347eece71399a7e2e907ea3df4f");
    }

    [Test]
    public void Compute_DifferentInputs_ProduceDifferentShas()
    {
        var a = GitBlobHasher.Compute(Encoding.UTF8.GetBytes("a"));
        var b = GitBlobHasher.Compute(Encoding.UTF8.GetBytes("b"));
        a.Should().NotBe(b);
    }
}
