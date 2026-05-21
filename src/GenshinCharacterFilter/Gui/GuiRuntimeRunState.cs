namespace GenshinCharacterFilter.Gui;

/// <summary>
/// User-facing runtime state for the persistent WPF control dock.
/// </summary>
public enum GuiRuntimeRunState
{
    Idle,
    Starting,
    Detecting,
    Reduced,
    Restored,
    CaptureLost,
    Stopping,
    Error
}
