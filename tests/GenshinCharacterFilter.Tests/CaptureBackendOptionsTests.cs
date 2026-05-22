using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class CaptureBackendOptionsTests
{
    [Theory]
    [InlineData(CaptureBackend.VisiblePixels)]
    [InlineData(CaptureBackend.WindowsGraphicsCapture)]
    public void Validate_AllowsSupportedBackends(CaptureBackend backend)
    {
        CaptureBackendOptions options = new() { Backend = backend };

        options.Validate();
    }

    [Fact]
    public void Validate_RejectsInvalidBackend()
    {
        CaptureBackendOptions options = new() { Backend = (CaptureBackend)999 };

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Contains("Capture backend", exception.Message);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(30001)]
    public void Validate_RejectsInvalidTimeout(int timeoutMs)
    {
        CaptureBackendOptions options = new() { CaptureTimeoutMs = timeoutMs };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }
}
