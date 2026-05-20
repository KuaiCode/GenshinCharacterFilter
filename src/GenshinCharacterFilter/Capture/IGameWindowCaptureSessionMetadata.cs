namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Optional diagnostic metadata for live capture sessions.
/// </summary>
public interface IGameWindowCaptureSessionMetadata
{
    string CaptureModePrefix { get; }
}
