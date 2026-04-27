using CHAP2.Domain.Enums;
using CHAP2.Domain.Exceptions;

namespace CHAP2.Domain.Entities;

public class UserPreferences : IEquatable<UserPreferences>
{
    public string UserId { get; private set; } = string.Empty;
    public Theme Theme { get; private set; }
    public SearchScope DefaultSearchScope { get; private set; }
    public Language Language { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private UserPreferences() { }

    internal static UserPreferences Reconstitute(
        string userId,
        Theme theme,
        SearchScope defaultSearchScope,
        Language language,
        DateTime updatedAt)
    {
        return new UserPreferences
        {
            UserId = userId,
            Theme = theme,
            DefaultSearchScope = defaultSearchScope,
            Language = language,
            UpdatedAt = updatedAt,
        };
    }

    public static UserPreferences Default(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("User ID cannot be empty.");

        return new UserPreferences
        {
            UserId = userId,
            Theme = Theme.System,
            DefaultSearchScope = SearchScope.All,
            Language = Language.En,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(Theme theme, SearchScope defaultSearchScope, Language language)
    {
        Theme = theme;
        DefaultSearchScope = defaultSearchScope;
        Language = language;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool Equals(UserPreferences? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserId.Equals(other.UserId, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as UserPreferences);

    public override int GetHashCode() => UserId.GetHashCode(StringComparison.Ordinal);
}
