namespace GenshinCharacterFilter.Capture;

public enum WindowCaptureFailureReason
{
    Unknown,
    TargetWindowMinimizedCannotRestore,
    ForegroundWindowProcessMismatch
}

/// <summary>
/// Represents a recoverable window capture failure with a user-facing message.
/// </summary>
public sealed class WindowCaptureException : Exception
{
    public WindowCaptureException(string message)
        : this(message, WindowCaptureFailureReason.Unknown)
    {
    }

    public WindowCaptureException(string message, WindowCaptureFailureReason reason)
        : base(message)
    {
        Reason = reason;
    }

    public WindowCaptureException(string message, Exception innerException)
        : this(message, WindowCaptureFailureReason.Unknown, innerException)
    {
    }

    public WindowCaptureException(string message, WindowCaptureFailureReason reason, Exception innerException)
        : base(message, innerException)
    {
        Reason = reason;
    }

    public WindowCaptureFailureReason Reason { get; }

    public static WindowCaptureException TargetWindowMinimizedCannotRestore(string processName)
    {
        return new WindowCaptureException(
            $"Target window for process '{processName}' is minimized and could not be restored. " +
            "This app currently captures visible screen pixels, so the target window must be visible and not minimized. " +
            "Restore the target window manually, keep it uncovered, or use borderless/windowed mode before capturing. " +
            "Exclusive fullscreen windows may minimize or fail visible-pixel capture when focus changes.",
            WindowCaptureFailureReason.TargetWindowMinimizedCannotRestore);
    }

    public static WindowCaptureException ForegroundWindowProcessMismatch(string expectedProcessName, string actualProcessName)
    {
        return new WindowCaptureException(
            $"Current foreground window is not '{expectedProcessName}'; it belongs to '{actualProcessName}'. " +
            $"Switch to the '{expectedProcessName}' window and keep it visible before retrying calibration.",
            WindowCaptureFailureReason.ForegroundWindowProcessMismatch);
    }
}
