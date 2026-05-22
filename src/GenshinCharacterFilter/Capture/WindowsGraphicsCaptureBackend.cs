namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Isolated Windows.Graphics.Capture spike boundary.
/// </summary>
public sealed class WindowsGraphicsCaptureBackend : IGameCaptureBackend
{
    private readonly TextWriter _log;

    public WindowsGraphicsCaptureBackend(TextWriter? log = null)
    {
        _log = log ?? TextWriter.Null;
    }

    public CaptureBackend Backend => CaptureBackend.WindowsGraphicsCapture;

    public string StatusLabel => CheckAvailability().Available ? "Available" : "Unavailable";

    public CaptureBackendAvailability CheckAvailability()
    {
        if (!OperatingSystem.IsWindows())
        {
            return CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.UnsupportedOS,
                "Windows.Graphics.Capture is only available on Windows.");
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
        {
            return CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.UnsupportedOS,
                "Windows.Graphics.Capture requires Windows 10 version 1903 or newer.");
        }

        return CaptureBackendAvailability.Unavailable(
            CaptureBackendFailureReason.BackendUnavailable,
            "Windows.Graphics.Capture backend spike is isolated, but frame acquisition is not enabled in this build. Use VisiblePixels or enable explicit backend fallback.");
    }

    public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowUnavailable();
        return Task.FromResult(string.Empty);
    }

    public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
    {
        ThrowUnavailable();
        throw new UnreachableException();
    }

    private void ThrowUnavailable()
    {
        CaptureBackendAvailability availability = CheckAvailability();
        _log.WriteLine($"WindowsGraphicsCapture unavailable: {availability.Message}");
        throw new CaptureBackendException(
            CaptureBackend.WindowsGraphicsCapture,
            availability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
            availability.Message);
    }

    private sealed class UnreachableException : Exception
    {
    }
}
