namespace FlashFixPro.Core;

public sealed class ReadOnlyFileSignatureRecoveryEngine : IFileRecoveryEngine
{
    public Task<IReadOnlyList<RecoveryCandidate>> ScanAsync(
        StorageDevice device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (device.IsProtected)
        {
            throw new InvalidOperationException(
                "Read-only recovery cannot scan a protected system disk.");
        }

        return Task.FromResult<IReadOnlyList<RecoveryCandidate>>(
        [
            new RecoveryCandidate(
                "Recovery scanner ready",
                "Disk image or supported volume required",
                0,
                "No third-party recovery engine is bundled yet; use a disk image for offline analysis.",
                0)
        ]);
    }
}
