using System.IO;
using System.Windows;
using System.Windows.Controls;
using FlashFixPro.Core;

namespace FlashFixPro.App;

public partial class MainWindow : Window
{
    private readonly IDeviceDiscoveryService _discovery = new WindowsDeviceDiscoveryService();
    private readonly IDiagnosticEngine _diagnostics = new ReadOnlyDiagnosticEngine();
    private readonly IFileRecoveryEngine _recovery = new ReadOnlyFileSignatureRecoveryEngine();
    private readonly ICapacityVerificationEngine _capacity = new TestImageCapacityEngine();
    private readonly OperationJournal _journal = new();

    public MainWindow()
    {
        InitializeComponent();
        OperationLog.ItemsSource = _journal.Entries;
        ScanButton.Click += async (_, _) => await ScanDevicesAsync();
        DiagnoseButton.Click += async (_, _) => await DiagnoseSelectedAsync();
        RecoveryButton.Click += async (_, _) => await ScanRecoveryAsync();
        CapacityButton.Click += async (_, _) => await VerifyTestImageAsync();
    }

    public sealed record DeviceView(
        StorageDevice Device,
        string Model,
        int DiskNumber,
        string InterfaceType,
        string DisplaySize,
        string StatusText,
        string SafetyText);

    private async Task ScanDevicesAsync()
    {
        _journal.Append("Scanning physical disks and volumes.");
        try
        {
            BeginOperation("Scanning devices...");
            var devices = await _discovery.DiscoverAsync();
            DeviceList.ItemsSource = devices.Select(device => new DeviceView(
                device,
                string.IsNullOrWhiteSpace(device.Model) ? "Unknown storage device" : device.Model.Trim(),
                device.DiskNumber,
                string.IsNullOrWhiteSpace(device.InterfaceType) ? "Unknown interface" : device.InterfaceType,
                device.DisplaySize,
                device.Volumes.Count == 0 ? "No mounted volume" :
                    string.Join(", ", device.Volumes.Select(volume =>
                        $"{volume.DriveLetter} {volume.FileSystem}".Trim())),
                device.IsProtected ? "Protected" : "Eligible")).ToList();
            _journal.Append($"Scan complete: {devices.Count} physical disk(s).");
        }
        catch (Exception exception)
        {
            _journal.Append($"Discovery failed: {exception.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    private void DeviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DeviceList.SelectedItem is not DeviceView view) return;
        SelectedName.Text = view.Model;
        SelectedDetails.Text =
            $"Disk {view.DiskNumber} • {view.InterfaceType}\n" +
            $"{view.DisplaySize} • {view.SafetyText}\n" +
            $"Serial: {view.Device.SerialNumber}";
    }

    private StorageDevice? GetSelectedDevice() =>
        DeviceList.SelectedItem as DeviceView is { Device: var device } ? device : null;

    private async Task DiagnoseSelectedAsync()
    {
        var device = GetSelectedDevice();
        if (device is null) { _journal.Append("Select a device before diagnosis."); OperationLog.Items.Refresh(); return; }
        try
        {
            BeginOperation($"Diagnosing disk {device.DiskNumber}...");
            _journal.Append($"Running read-only diagnosis on disk {device.DiskNumber}.");
            var report = await _diagnostics.DiagnoseAsync(device);
            foreach (var finding in report.Findings)
                _journal.Append($"{finding.Severity}: {finding.Summary}");
        }
        catch (Exception exception)
        {
            _journal.Append($"Diagnosis failed: {exception.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task ScanRecoveryAsync()
    {
        var device = GetSelectedDevice();
        if (device is null) { _journal.Append("Select a device before recovery scanning."); OperationLog.Items.Refresh(); return; }
        try
        {
            _journal.Append($"Starting read-only recovery scan on disk {device.DiskNumber}.");
            BeginOperation($"Scanning disk {device.DiskNumber} read-only...");
            var candidates = await _recovery.ScanAsync(device);
            foreach (var candidate in candidates)
                _journal.Append($"{candidate.Name}: {candidate.Reason}");
        }
        catch (Exception exception)
        {
            _journal.Append($"Recovery scan blocked: {exception.Message}");
        }
        finally { EndOperation(); }
    }

    private async Task VerifyTestImageAsync()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FlashFixPro");
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, $"capacity-{Guid.NewGuid():N}.bin");
        try
        {
            BeginOperation("Verifying an isolated 8 MB test image...");
            _journal.Append("Verifying capacity engine with an isolated 8 MB test image.");
            var result = await _capacity.VerifyTestImageAsync(file, 8 * 1024 * 1024);
            foreach (var entry in result.Log ?? [])
                _journal.Append(entry);
            _journal.Append(result.Message);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
            EndOperation();
        }
    }

    private void BeginOperation(string status)
    {
        OperationStatus.Text = status;
        OperationProgress.Visibility = Visibility.Visible;
        ScanButton.IsEnabled = false;
        DiagnoseButton.IsEnabled = false;
        RecoveryButton.IsEnabled = false;
        CapacityButton.IsEnabled = false;
        OperationLog.Items.Refresh();
    }

    private void EndOperation()
    {
        OperationStatus.Text = "RashiTec tools ready.";
        OperationProgress.Visibility = Visibility.Collapsed;
        ScanButton.IsEnabled = true;
        DiagnoseButton.IsEnabled = true;
        RecoveryButton.IsEnabled = true;
        CapacityButton.IsEnabled = true;
        OperationLog.Items.Refresh();
    }
}