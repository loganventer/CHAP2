using CHAP2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// First-start setup of the chorus data directory. Idempotent.
///
/// Two paths depending on whether GitHub sync is enabled:
///   - Enabled: pull every chorus JSON from the configured remote
///     into the data directory via <see cref="IChorusGitHubSync"/>.
///   - Disabled (offline / dev container): seed from the baked-in
///     image data when the data directory is empty.
///
/// Composition over inheritance: depends on the narrow sync contract
/// and the file system; doesn't know about HTTP, git, or schedules.
/// </summary>
public sealed class ChorusDiskBootstrapper : IChorusDiskBootstrapper
{
    private readonly bool _gitHubSyncEnabled;
    private readonly string _dataDirectory;
    private readonly string _imageSeedDirectory;
    private readonly IChorusGitHubSync _sync;
    private readonly ILogger<ChorusDiskBootstrapper> _logger;

    public ChorusDiskBootstrapper(
        bool gitHubSyncEnabled,
        string dataDirectory,
        string imageSeedDirectory,
        IChorusGitHubSync sync,
        ILogger<ChorusDiskBootstrapper> logger)
    {
        _gitHubSyncEnabled = gitHubSyncEnabled;
        _dataDirectory = !string.IsNullOrWhiteSpace(dataDirectory)
            ? dataDirectory
            : throw new ArgumentException("dataDirectory required", nameof(dataDirectory));
        _imageSeedDirectory = imageSeedDirectory ?? string.Empty;
        _sync = sync ?? throw new ArgumentNullException(nameof(sync));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_dataDirectory);

        if (_gitHubSyncEnabled)
        {
            try
            {
                await _sync.BootstrapAsync(_dataDirectory, cancellationToken);
                return;
            }
            catch (Exception ex)
            {
                // Bootstrap is best-effort; never crash the process. The
                // daily worker / force-sync UI will retry, and the disk
                // is still usable with whatever was already there.
                _logger.LogError(ex, "GitHub bootstrap failed; continuing with whatever's on disk");
                return;
            }
        }

        if (Directory.EnumerateFileSystemEntries(_dataDirectory).Any())
        {
            _logger.LogDebug("Chorus disk already populated; skipping image seed");
            return;
        }
        if (!Directory.Exists(_imageSeedDirectory))
        {
            _logger.LogWarning("No image-seed directory at {Path}; chorus disk left empty", _imageSeedDirectory);
            return;
        }
        var copied = SeedFromImage(_imageSeedDirectory, _dataDirectory);
        _logger.LogInformation("Seeded {Count} chorus file(s) from image into {Target}", copied, _dataDirectory);
    }

    private static int SeedFromImage(string source, string target)
    {
        var copied = 0;
        foreach (var file in Directory.EnumerateFiles(source, "*.json", SearchOption.TopDirectoryOnly))
        {
            var dest = Path.Combine(target, Path.GetFileName(file));
            File.Copy(file, dest, overwrite: false);
            copied++;
        }
        return copied;
    }
}
