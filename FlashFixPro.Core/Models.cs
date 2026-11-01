namespace FlashFixPro.Core;

public sealed record StorageVolume(
    string DriveLetter,
    string Label,
    string FileSystem,
    ulong CapacityBytes,
    ulong FreeBytes);

public sealed record StorageDevice(
    int DiskNumber,
    string Model,
    string SerialNumber,
    string InterfaceType,
    string MediaType,
    ulong SizeBytes,
    bool IsRemovable,
    bool IsSystemDisk,
    bool IsProtected,
    IReadOnlyList<StorageVolume> Volumes)
{
    public string DisplaySize => FormatSize(SizeBytes);

    public static string FormatSize(ulong bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}

public sealed record DiagnosticFinding(
    string Code,
    string Severity,
    string Summary,
    string RecommendedAction);

public sealed record DiagnosticReport(
    StorageDevice Device,
    IReadOnlyList<DiagnosticFinding> Findings,
    bool ReadOnlyScanCompleted);

public sealed record RecoveryCandidate(
    string Name,
    string Path,
    ulong Size,
    string Reason,
    double Confidence);

public sealed record OperationResult(
    bool Success,
    string Message,
    IReadOnlyList<string>? Log = null);
