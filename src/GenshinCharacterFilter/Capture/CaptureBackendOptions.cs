namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Runtime settings for selecting and diagnosing a live capture backend.
/// </summary>
public sealed class CaptureBackendOptions
{
    public const int DefaultCaptureTimeoutMs = 2000;
    public const int MinCaptureTimeoutMs = 100;
    public const int MaxCaptureTimeoutMs = 30000;

    public CaptureBackend Backend { get; set; } = CaptureBackend.VisiblePixels;

    public bool AllowBackendFallback { get; set; }

    public int CaptureTimeoutMs { get; set; } = DefaultCaptureTimeoutMs;

    public void Validate()
    {
        if (!Enum.IsDefined(Backend))
        {
            throw new ArgumentException("Capture backend is not supported.", nameof(Backend));
        }

        if (CaptureTimeoutMs is < MinCaptureTimeoutMs or > MaxCaptureTimeoutMs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CaptureTimeoutMs),
                $"Capture timeout must be between {MinCaptureTimeoutMs} and {MaxCaptureTimeoutMs} ms.");
        }
    }

    public CaptureBackendOptions Clone()
    {
        return new CaptureBackendOptions
        {
            Backend = Backend,
            AllowBackendFallback = AllowBackendFallback,
            CaptureTimeoutMs = CaptureTimeoutMs
        };
    }
}
