namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Error thrown when a selected capture backend cannot initialize or acquire a frame.
/// </summary>
public sealed class CaptureBackendException : Exception
{
    public CaptureBackendException(
        CaptureBackend backend,
        CaptureBackendFailureReason reason,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Backend = backend;
        Reason = reason;
    }

    public CaptureBackend Backend { get; }

    public CaptureBackendFailureReason Reason { get; }
}
