using System.Security.Cryptography;
using System.Text;

namespace CHAP2.Application.Helpers;

/// <summary>
/// Computes git's blob SHA-1 -- SHA1("blob {len}\0" + content). Used to
/// match GitHub's `sha` field on tree entries without shelling out to
/// `git hash-object`. Pure, allocation-light, single-purpose.
/// </summary>
public static class GitBlobHasher
{
    public static string Compute(ReadOnlySpan<byte> content)
    {
        var prefix = Encoding.ASCII.GetBytes($"blob {content.Length}\0");
        using var sha = SHA1.Create();
        sha.TransformBlock(prefix, 0, prefix.Length, null, 0);
        // SHA1 needs a heap buffer for the final block; copy span -> array.
        var buf = content.ToArray();
        sha.TransformFinalBlock(buf, 0, buf.Length);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }
}
