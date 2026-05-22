namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Immutable state rendered by the WPF persistent control dock.
/// </summary>
public readonly record struct GuiStatusSnapshot(
    GuiRuntimeRunState RunState,
    GuiAudioState AudioState,
    string CaptureBackend,
    string CaptureStatus,
    string LastOcrText,
    string LastDetectedSpeaker,
    string LastAudioAction);
