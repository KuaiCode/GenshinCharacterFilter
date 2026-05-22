namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Live capture backend boundary used by CLI and GUI command services.
/// </summary>
public interface IGameCaptureBackend : IGameWindowCapture, IGameWindowCaptureSessionFactory
{
    CaptureBackend Backend { get; }

    string StatusLabel { get; }

    CaptureBackendAvailability CheckAvailability();
}
