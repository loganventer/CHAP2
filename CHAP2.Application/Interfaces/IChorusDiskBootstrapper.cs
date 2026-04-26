namespace CHAP2.Application.Interfaces;

/// <summary>
/// First-start setup of the chorus data directory. Idempotent --
/// re-running on subsequent boots is a fast-path no-op.
///
/// Two paths depending on whether git-sync is enabled:
///   - Enabled: clone the configured remote into the data directory
///     when it isn't already a working tree.
///   - Disabled (offline / dev container): seed from the baked-in
///     image data when the data directory is empty.
/// </summary>
public interface IChorusDiskBootstrapper
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
}
