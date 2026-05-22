namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Describes whether a concrete capture backend can run in the current environment.
/// </summary>
public sealed record CaptureBackendAvailability(
    bool Available,
    CaptureBackendFailureReason? FailureReason,
    string Message)
{
    public static CaptureBackendAvailability Ready(string message) => new(true, null, message);

    public static CaptureBackendAvailability Unavailable(
        CaptureBackendFailureReason reason,
        string message) => new(false, reason, message);
}
