using System.Text.Json;

namespace FlashFixPro.Core;

public interface IDeviceDiscoveryService
{
    Task<IReadOnlyList<StorageDevice>> DiscoverAsync(
        CancellationToken cancellationToken = default);
}

public interface IDiagnosticEngine
{
    Task<DiagnosticReport> DiagnoseAsync(
        StorageDevice device,
        CancellationToken cancellationToken = default);
}

public interface IFileRecoveryEngine
{
    Task<IReadOnlyList<RecoveryCandidate>> ScanAsync(
        StorageDevice device,
        CancellationToken cancellationToken = default);
}

public interface ICapacityVerificationEngine
{
    Task<OperationResult> VerifyTestImageAsync(
        string imagePath,
        long sizeBytes,
        CancellationToken cancellationToken = default);
}

public interface IOperationJournal
{
    void Append(string message);
    IReadOnlyList<string> Entries { get; }
}
