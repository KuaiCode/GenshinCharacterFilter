namespace GenshinCharacterFilter.Gui;

/// <summary>
/// UI-independent restore policy for manual foreground fallback.
/// </summary>
public static class GuiManualForegroundFallbackFlow
{
    public static bool ShouldRestoreAfterSessionReady => false;

    public static bool ShouldRestoreAfterOperationCompleted => true;

    public static bool ShouldRestoreAfterSessionFailure => true;

    public static bool ShouldRestoreAfterUserCancel => true;
}
