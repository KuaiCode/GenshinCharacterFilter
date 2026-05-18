namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Captures repeated screenshots from one live target window session.
/// </summary>
public interface IGameWindowCaptureSession : IDisposable
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task<string> CaptureAsync(CancellationToken cancellationToken);

    Task<WindowCaptureFrameInfo> GetFrameInfoAsync(CancellationToken cancellationToken);

    Task<string> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken);
}
