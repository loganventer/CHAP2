namespace CHAP2.Shared.DTOs;

public class UserPreferencesDto
{
    public string UserId { get; set; } = string.Empty;
    public string Theme { get; set; } = string.Empty;
    public string DefaultSearchScope { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
