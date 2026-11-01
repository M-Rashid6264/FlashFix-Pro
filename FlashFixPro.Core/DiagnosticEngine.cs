namespace FlashFixPro.Core;

public sealed class ReadOnlyDiagnosticEngine : IDiagnosticEngine
{
    public Task<DiagnosticReport> DiagnoseAsync(
        StorageDevice device,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<DiagnosticFinding>();

        if (device.IsProtected)
        {
            findings.Add(new DiagnosticFinding(
                "PROTECTED_DISK",
                "High",
                "This disk is protected and cannot be selected for destructive operations.",
                "Select only a removable, verified non-system disk."));
        }

        if (!device.Volumes.Any())
        {
            findings.Add(new DiagnosticFinding(
                "NO_VOLUME",
                "Medium",
                "No mounted volume was found on this disk.",
                "Check partition assignment, RAW status, and cable or card-reader connection."));
        }
        else if (device.Volumes.Any(v =>
                     string.Equals(v.FileSystem, "RAW", StringComparison.OrdinalIgnoreCase)
                     || string.IsNullOrWhiteSpace(v.FileSystem)))
        {
            findings.Add(new DiagnosticFinding(
                "RAW_VOLUME",
                "Medium",
                "A volume is present but Windows does not recognize its file system.",
                "Create a disk image before repair; then consider recovery or file-system repair."));
        }
        else
        {
            findings.Add(new DiagnosticFinding(
                "VOLUME_VISIBLE",
                "Info",
                "At least one Windows-recognized volume is mounted.",
                "Run a read-only scan before using repair tools."));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new DiagnosticReport(device, findings, true));
    }
}
