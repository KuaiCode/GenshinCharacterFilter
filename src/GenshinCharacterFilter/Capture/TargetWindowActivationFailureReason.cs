namespace GenshinCharacterFilter.Capture;

public enum TargetWindowActivationFailureReason
{
    None,
    TargetNotFound,
    StillMinimized,
    ForegroundMismatch,
    ActivationDenied,
    TimedOut,
    UnknownError
}
