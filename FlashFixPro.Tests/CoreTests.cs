using FlashFixPro.Core;

namespace FlashFixPro.Tests;

public class CoreTests
{
    [Fact]
    public async Task DiagnosticEngineFlagsSystemDiskAsProtected()
    {
        var device = TestDevice();
        var report = await new ReadOnlyDiagnosticEngine().DiagnoseAsync(device);

        Assert.Contains(report.Findings, f => f.Code == "PROTECTED_DISK");
        Assert.True(report.ReadOnlyScanCompleted);
    }

    [Fact]
    public async Task RecoveryEngineRejectsProtectedDisk()
    {
        var engine = new ReadOnlyFileSignatureRecoveryEngine();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => engine.ScanAsync(TestDevice()));
    }

    [Fact]
    public async Task CapacityEngineVerifiesTestImage()
    {
        var path = Path.Combine(Path.GetTempPath(), $"flashfix-{Guid.NewGuid():N}.bin");
        try
        {
            var result = await new TestImageCapacityEngine()
                .VerifyTestImageAsync(path, 2 * 1024 * 1024);
            Assert.True(result.Success);
            Assert.Equal(2 * 1024 * 1024, new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(0UL, "0 B")]
    [InlineData(1024UL, "1 KB")]
    [InlineData(5UL * 1024 * 1024 * 1024, "5 GB")]
    public void FormatSizeProducesReadableValue(ulong bytes, string expected)
    {
        Assert.Equal(expected, StorageDevice.FormatSize(bytes));
    }

    private static StorageDevice TestDevice() => new(
        0,
        "Test disk",
        "TEST-SERIAL",
        "USB",
        "Removable Media",
        1024,
        true,
        true,
        true,
        [new StorageVolume("E:", "TEST", "NTFS", 1024, 512)]);
}
