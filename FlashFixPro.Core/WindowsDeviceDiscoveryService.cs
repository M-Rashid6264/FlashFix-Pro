using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FlashFixPro.Core;

public sealed class WindowsDeviceDiscoveryService : IDeviceDiscoveryService
{
    private const string Script = """
$ErrorActionPreference = 'Stop'
$systemDisk = $null
try {
  $boot = Get-CimInstance Win32_OperatingSystem | Select-Object -First 1
  $systemDrive = $env:SystemDrive.TrimEnd(':')
  $systemDisk = (Get-CimInstance Win32_DiskPartition -Filter "DriveLetter = '$env:SystemDrive'" |
    Select-Object -First 1).DiskIndex
} catch {}
$disks = Get-CimInstance Win32_DiskDrive
$result = foreach ($disk in $disks) {
  $parts = Get-CimInstance Win32_DiskPartition -Filter "DiskIndex = $($disk.Index)"
  $volumes = foreach ($part in $parts) {
    Get-CimInstance Win32_LogicalDiskToPartition |
      Where-Object { $_.Antecedent.DeviceID -eq $part.DeviceID } |
      ForEach-Object {
        $volume = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID = '$($_.Dependent.DeviceID)'"
        if ($volume) {
          [PSCustomObject]@{
            DriveLetter = $volume.DeviceID
            Label = $volume.VolumeName
            FileSystem = $volume.FileSystem
            CapacityBytes = [uint64]$volume.Size
            FreeBytes = [uint64]$volume.FreeSpace
          }
        }
      }
  }
  [PSCustomObject]@{
    DiskNumber = [int]$disk.Index
    Model = $disk.Model
    SerialNumber = $disk.SerialNumber
    InterfaceType = $disk.InterfaceType
    MediaType = $disk.MediaType
    SizeBytes = [uint64]$disk.Size
    IsRemovable = (($disk.MediaType -match 'Removable') -or ($disk.InterfaceType -match 'USB'))
    IsSystemDisk = ([int]$disk.Index -eq [int]$systemDisk)
    IsProtected = (($disk.MediaType -notmatch 'Removable') -and ($disk.InterfaceType -notmatch 'USB')) -or ([int]$disk.Index -eq [int]$systemDisk)
    Volumes = @($volumes)
  }
}
$result | ConvertTo-Json -Depth 5 -Compress
""";

    public async Task<IReadOnlyList<StorageDevice>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-EncodedCommand");
        startInfo.ArgumentList.Add(Convert.ToBase64String(Encoding.Unicode.GetBytes(Script)));

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Windows PowerShell could not be started.");
        using (cancellationToken.Register(() =>
               {
                   try { process.Kill(true); } catch { }
               }))
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Device discovery failed: {error.Trim()}");
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                return [];
            }

            using var document = JsonDocument.Parse(output.TrimStart('\0', '\r', '\n'));
            var root = document.RootElement;
            var elements = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : root.EnumerateObject().Any() ? new[] { root }.AsEnumerable() : [];

            var devices = new List<StorageDevice>();
            foreach (var element in elements)
            {
                var volumes = new List<StorageVolume>();
                if (element.TryGetProperty("Volumes", out var volumeArray))
                {
                    if (volumeArray.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var volume in volumeArray.EnumerateArray())
                            volumes.Add(ReadVolume(volume));
                    }
                    else if (volumeArray.ValueKind == JsonValueKind.Object)
                    {
                        volumes.Add(ReadVolume(volumeArray));
                    }
                }

                devices.Add(new StorageDevice(
                    element.GetProperty("DiskNumber").GetInt32(),
                    element.GetProperty("Model").GetString() ?? "Unknown",
                    element.GetProperty("SerialNumber").GetString() ?? "",
                    element.GetProperty("InterfaceType").GetString() ?? "",
                    element.GetProperty("MediaType").GetString() ?? "",
                    GetUInt64(element, "SizeBytes"),
                    GetBool(element, "IsRemovable"),
                    GetBool(element, "IsSystemDisk"),
                    GetBool(element, "IsProtected"),
                    volumes));
            }

            return devices;
        }
    }

    private static StorageVolume ReadVolume(JsonElement volume) => new(
        volume.GetProperty("DriveLetter").GetString() ?? "",
        volume.GetProperty("Label").GetString() ?? "",
        volume.GetProperty("FileSystem").GetString() ?? "Unknown",
        GetUInt64(volume, "CapacityBytes"),
        GetUInt64(volume, "FreeBytes"));

    private static ulong GetUInt64(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return 0;
        return value.ValueKind == JsonValueKind.Number
            ? value.GetUInt64()
            : ulong.TryParse(value.GetString(), out var parsed) ? parsed : 0;
    }

    private static bool GetBool(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.True;
}
