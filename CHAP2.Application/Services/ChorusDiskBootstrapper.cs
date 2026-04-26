using CHAP2.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CHAP2.Application.Services;

/// <summary>
/// First-start setup of the chorus data directory. Idempotent.
///
/// When git-sync is enabled the bootstrapper composes
/// <see cref="IGitWorkingTree"/> to clone the remote into the data
/// directory. When disabled (offline / dev container) it falls back to
/// copying the baked-in image data into the directory if the directory
/// is empty.
///
/// One responsibility (Single Responsibility): get the disk to a usable
/// state. The orchestrator handles ongoing sync.
/// </summary>
public sealed class ChorusDiskBootstrapper : IChorusDiskBootstrapper
{
    private readonly bool _gitSyncEnabled;
    private readonly string _dataDirectory;
    private readonly string _imageSeedDirectory;
    private readonly IGitWorkingTree _tree;
    private readonly ILogger<ChorusDiskBootstrapper> _logger;

    public ChorusDiskBootstrapper(
        bool gitSyncEnabled,
        string dataDirectory,
        string imageSeedDirectory,
        IGitWorkingTree tree,
        ILogger<ChorusDiskBootstrapper> logger)
    {
        _gitSyncEnabled = gitSyncEnabled;
        _dataDirectory = !string.IsNullOrWhiteSpace(dataDirectory)
            ? dataDirectory
            : throw new ArgumentException("dataDirectory required", nameof(dataDirectory));
        _imageSeedDirectory = imageSeedDirectory ?? string.Empty;
        _tree = tree ?? throw new ArgumentNullException(nameof(tree));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_dataDirectory);

        if (_gitSyncEnabled)
        {
            if (await _tree.IsClonedAsync(cancellationToken))
            {
                _logger.LogDebug("Chorus disk already a git working tree at {Path}", _dataDirectory);
                return;
            }
            _logger.LogInformation("Cloning chorus repo into {Path}", _dataDirectory);
            await _tree.EnsureCloneAsync(cancellationToken);
            return;
        }

        // Git-sync disabled (e.g. local dev / offline Docker). Seed from
        // the baked-in image data only when the disk is empty -- never
        // clobber existing files.
        if (Directory.EnumerateFileSystemEntries(_dataDirectory).Any())
        {
            _logger.LogDebug("Chorus disk already populated; skipping image seed");
            return;
        }
        if (!Directory.Exists(_imageSeedDirectory))
        {
            _logger.LogWarning("No image-seed directory found at {Path}; chorus disk left empty", _imageSeedDirectory);
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
