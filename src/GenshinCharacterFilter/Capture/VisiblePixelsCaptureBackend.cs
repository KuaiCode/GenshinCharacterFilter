namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Existing visible-screen-pixels capture backend based on the current Win32/BitBlt path.
/// </summary>
public sealed class VisiblePixelsCaptureBackend : IGameCaptureBackend
{
    private readonly WindowsGameWindowCapture _capture;

    public VisiblePixelsCaptureBackend(TextWriter? log = null)
    {
        _capture = new WindowsGameWindowCapture(log);
    }

    public CaptureBackend Backend => CaptureBackend.VisiblePixels;

    public string StatusLabel => "Ready";

    public CaptureBackendAvailability CheckAvailability()
    {
        return OperatingSystem.IsWindows()
            ? CaptureBackendAvailability.Ready("VisiblePixels capture backend is available.")
            : CaptureBackendAvailability.Unavailable(
                CaptureBackendFailureReason.UnsupportedOS,
                "VisiblePixels capture backend is only supported on Windows.");
    }

    public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        return _capture.CaptureOnceAsync(options, cancellationToken);
    }

    public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
    {
        return _capture.CreateSession(options);
    }

    public IGameWindowCaptureSession CreateForegroundSession(WindowCaptureOptions options)
    {
        return _capture.CreateForegroundSession(options);
    }
}
