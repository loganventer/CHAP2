namespace CHAP2.Shared.DTOs;

public class UserSettingsDto
{
    public string UserId { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
