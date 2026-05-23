namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Immutable state rendered by the WPF persistent control dock.
/// </summary>
public readonly record struct GuiStatusSnapshot(
    GuiRuntimeRunState RunState,
    GuiAudioState AudioState,
    string RequestedCaptureBackend,
    string ActualCaptureBackend,
    string CaptureStatus,
    string LastOcrText,
    string LastDetectedSpeaker,
    string LastAudioAction)
{
    public string CaptureBackend => string.Equals(RequestedCaptureBackend, ActualCaptureBackend, StringComparison.Ordinal)
        ? RequestedCaptureBackend
        : $"Requested: {RequestedCaptureBackend}; actual: {ActualCaptureBackend}";
}
