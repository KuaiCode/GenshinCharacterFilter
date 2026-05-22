using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter;

/// <summary>
/// Stores live capture backend defaults loaded from local configuration.
/// </summary>
public sealed class AppCaptureSettings
{
    public CaptureBackend Backend { get; set; } = CaptureBackend.VisiblePixels;

    public bool AllowBackendFallback { get; set; }

    public int CaptureTimeoutMs { get; set; } = CaptureBackendOptions.DefaultCaptureTimeoutMs;

    public void Validate()
    {
        try
        {
            ToOptions().Validate();
        }
        catch (ArgumentException exception)
        {
            throw new AppSettingsException(exception.Message, exception);
        }
    }

    public CaptureBackendOptions ToOptions()
    {
        return new CaptureBackendOptions
        {
            Backend = Backend,
            AllowBackendFallback = AllowBackendFallback,
            CaptureTimeoutMs = CaptureTimeoutMs
        };
    }
}
