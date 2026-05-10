namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Captures debug screenshots from a target game window.
/// </summary>
public interface IGameWindowCapture
{
    /// <summary>
    /// Captures the target window once and returns the saved screenshot path.
    /// </summary>
    Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken);
}
