using CHAP2.Application.Interfaces;

namespace CHAP2.Infrastructure.Repositories;

public sealed class DiskChorusFileGateway : IChorusFileGateway
{
    private readonly string _folderPath;

    public DiskChorusFileGateway(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("folderPath required", nameof(folderPath));
        _folderPath = Path.IsPathRooted(folderPath)
            ? folderPath
            : Path.Combine(Directory.GetCurrentDirectory(), folderPath);
    }

    public string GetFileName(Guid chorusId) => $"{chorusId}.json";

    public async Task<byte[]?> ReadAsync(Guid chorusId, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_folderPath, GetFileName(chorusId));
        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path, cancellationToken);
    }
}
