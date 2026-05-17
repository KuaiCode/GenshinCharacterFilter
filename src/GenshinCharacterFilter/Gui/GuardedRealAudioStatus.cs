namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Summarizes guarded real-audio readiness for display in the WinForms panel.
/// </summary>
public sealed record GuardedRealAudioStatus(
    string TargetProcessName,
    string AudioMode,
    int VolumePercent,
    bool HasOcrRegionSource,
    bool PreflightPassed,
    IReadOnlyList<string> Issues)
{
    public string Summary =>
        $"Target: {TargetProcessName}; Audio: {AudioMode}; Volume: {VolumePercent}%; Region: {(HasOcrRegionSource ? "configured" : "missing")}";
}
