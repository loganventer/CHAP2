using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

/// <summary>
/// Per-user UI preferences stored as an opaque JSON blob. The schema is
/// owned by the JS settings layer; the server only persists the bytes
/// and scopes them to the owning user. Empty payload is the implicit
/// default for a brand-new user.
/// </summary>
public class UserSettings : IEquatable<UserSettings>
{
    public string UserId { get; private set; } = string.Empty;
    public string Json { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }

    private UserSettings() { }

    internal static UserSettings Reconstitute(string userId, string json, DateTime updatedAt)
    {
        return new UserSettings
        {
            UserId = userId,
            Json = json ?? string.Empty,
            UpdatedAt = updatedAt,
        };
    }

    public static UserSettings Default(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty.");
        return new UserSettings
        {
            UserId = userId,
            Json = string.Empty,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string json)
    {
        Json = json ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool Equals(UserSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserId.Equals(other.UserId, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as UserSettings);
    public override int GetHashCode() => UserId.GetHashCode(StringComparison.Ordinal);
}
