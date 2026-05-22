namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Selects the live window capture implementation used by calibration and detection.
/// </summary>
public enum CaptureBackend
{
    VisiblePixels,
    WindowsGraphicsCapture
}
