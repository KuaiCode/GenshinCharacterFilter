namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Creates reusable live capture sessions for repeated detection loops.
/// </summary>
public interface IGameWindowCaptureSessionFactory
{
    IGameWindowCaptureSession CreateSession(WindowCaptureOptions options);
}
