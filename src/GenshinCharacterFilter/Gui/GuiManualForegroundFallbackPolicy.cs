using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Decides when WPF should offer manual foreground startup for live detection.
/// </summary>
public static class GuiManualForegroundFallbackPolicy
{
    public static bool ShouldPromptForDetection(Exception exception, bool useFixedImageForDetection)
    {
        return !useFixedImageForDetection &&
            exception is WindowCaptureException { Reason: WindowCaptureFailureReason.TargetWindowMinimizedCannotRestore };
    }
}
