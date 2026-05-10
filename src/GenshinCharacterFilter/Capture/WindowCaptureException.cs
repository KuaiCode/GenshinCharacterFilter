namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Represents a recoverable window capture failure with a user-facing message.
/// </summary>
public sealed class WindowCaptureException : Exception
{
    public WindowCaptureException(string message)
        : base(message)
    {
    }

    public WindowCaptureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
