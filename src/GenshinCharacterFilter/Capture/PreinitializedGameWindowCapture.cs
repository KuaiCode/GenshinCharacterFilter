namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Supplies a pre-initialized capture session to a detection loop.
/// </summary>
public sealed class PreinitializedGameWindowCapture : IGameWindowCapture, IGameWindowCaptureSessionFactory
{
    private IGameWindowCaptureSession? _session;

    public PreinitializedGameWindowCapture(IGameWindowCaptureSession session)
    {
        _session = session;
    }

    public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
    {
        if (_session is null)
        {
            throw new InvalidOperationException("The pre-initialized capture session has already been claimed.");
        }

        return _session.CaptureAsync(cancellationToken);
    }

    public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
    {
        IGameWindowCaptureSession session = _session
            ?? throw new InvalidOperationException("The pre-initialized capture session has already been claimed.");
        _session = null;
        return session;
    }
}
