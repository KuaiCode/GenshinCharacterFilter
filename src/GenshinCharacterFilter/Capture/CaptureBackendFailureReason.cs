namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Structured reasons for capture backend initialization or frame failures.
/// </summary>
public enum CaptureBackendFailureReason
{
    BackendUnavailable,
    UnsupportedOS,
    AccessDenied,
    TargetWindowInvalid,
    FrameTimeout,
    MinimizedNotSupported,
    UnknownError
}
