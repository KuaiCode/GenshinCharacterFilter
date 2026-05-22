using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class WindowsGraphicsCaptureBackendTests
{
    [Theory]
    [InlineData(99)]
    [InlineData(30001)]
    public void Constructor_RejectsInvalidTimeout(int timeoutMs)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WindowsGraphicsCaptureBackend(timeoutMs));
    }

    [Fact]
    public void CheckAvailability_ReturnsStructuredStatus()
    {
        WindowsGraphicsCaptureBackend backend = new();

        CaptureBackendAvailability availability = backend.CheckAvailability();

        if (availability.Available)
        {
            Assert.Null(availability.FailureReason);
            Assert.Contains("Windows.Graphics.Capture", availability.Message);
        }
        else
        {
            Assert.NotNull(availability.FailureReason);
            Assert.False(string.IsNullOrWhiteSpace(availability.Message));
        }
    }

    [Fact]
    public async Task CaptureOnce_TargetNotFoundUsesStructuredErrorWhenWgcIsAvailable()
    {
        WindowsGraphicsCaptureBackend backend = new();
        if (!backend.CheckAvailability().Available)
        {
            return;
        }

        WindowCaptureOptions options = new()
        {
            TargetProcessName = $"definitely-not-running-{Guid.NewGuid():N}",
            OutputDirectory = Path.GetTempPath()
        };

        CaptureBackendException exception = await Assert.ThrowsAsync<CaptureBackendException>(
            () => backend.CaptureOnceAsync(options, CancellationToken.None));

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, exception.Backend);
        Assert.Equal(CaptureBackendFailureReason.TargetWindowInvalid, exception.Reason);
    }
}
