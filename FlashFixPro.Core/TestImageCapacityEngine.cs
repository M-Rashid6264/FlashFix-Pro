namespace FlashFixPro.Core;

public sealed class TestImageCapacityEngine : ICapacityVerificationEngine
{
    public async Task<OperationResult> VerifyTestImageAsync(
        string imagePath,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        if (sizeBytes <= 0) return new OperationResult(false, "Test image size must be positive.");

        await using var stream = new FileStream(
            imagePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        stream.SetLength(sizeBytes);
        const int bufferSize = 1024 * 1024;
        var writeBuffer = new byte[bufferSize];
        var verifyBuffer = new byte[bufferSize];
        var random = new Random(0xF1A5);
        random.NextBytes(writeBuffer);

        for (var offset = 0L; offset < sizeBytes; offset += writeBuffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = (int)Math.Min(writeBuffer.Length, sizeBytes - offset);
            stream.Position = offset;
            await stream.WriteAsync(writeBuffer.AsMemory(0, count), cancellationToken);
        }

        await stream.FlushAsync(cancellationToken);
        for (var offset = 0L; offset < sizeBytes; offset += verifyBuffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = (int)Math.Min(verifyBuffer.Length, sizeBytes - offset);
            stream.Position = offset;
            var read = await stream.ReadAsync(verifyBuffer.AsMemory(0, count), cancellationToken);
            if (read != count) return new OperationResult(false, "The image ended before the requested size.");
            for (var index = 0; index < count; index++)
            {
                if (verifyBuffer[index] != writeBuffer[index])
                {
                    return new OperationResult(
                        false,
                        $"Data mismatch detected at image offset {offset + index}.");
                }
            }
        }

        return new OperationResult(
            true,
            $"Verified sequential pattern read/write for {StorageDevice.FormatSize((ulong)sizeBytes)}.",
            ["Test image size set", "Write pass completed", "Read-back verification completed"]);
    }
}
