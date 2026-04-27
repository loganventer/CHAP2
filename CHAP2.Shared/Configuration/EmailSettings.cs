namespace CHAP2.Shared.Configuration;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "CHAP2";
    public string PasswordResetUrlTemplate { get; set; } = string.Empty;
    public string EmailConfirmationUrlTemplate { get; set; } = string.Empty;
}
