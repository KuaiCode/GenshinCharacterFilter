namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Structured reasons for capture backend initialization or frame failures.
/// </summary>
public enum CaptureBackendFailureReason
{
    BackendUnavailable,
    UnsupportedOS,
    ApiUnavailable,
    Direct3DDeviceCreationFailed,
    CreateCaptureItemFailed,
    AccessDenied,
    TargetWindowInvalid,
    FrameTimeout,
    TargetMinimized,
    MinimizedNotSupported,
    UnknownError
}
