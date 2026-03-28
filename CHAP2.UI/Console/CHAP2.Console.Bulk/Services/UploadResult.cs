using CHAP2.Domain.Entities;

namespace CHAP2.Console.Bulk.Services;

public class UploadResult
{
    public bool Success { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public Chorus? Chorus { get; set; }
}
