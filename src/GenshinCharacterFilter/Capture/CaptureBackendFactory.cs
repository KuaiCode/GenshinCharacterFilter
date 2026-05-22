namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Creates configured live capture backend instances and applies explicit fallback policy.
/// </summary>
public sealed class CaptureBackendFactory
{
    private readonly Func<TextWriter?, IGameCaptureBackend> _visiblePixelsFactory;
    private readonly Func<TextWriter?, IGameCaptureBackend> _windowsGraphicsCaptureFactory;

    public CaptureBackendFactory()
        : this(
            log => new VisiblePixelsCaptureBackend(log),
            log => new WindowsGraphicsCaptureBackend(log))
    {
    }

    public CaptureBackendFactory(
        Func<TextWriter?, IGameCaptureBackend> visiblePixelsFactory,
        Func<TextWriter?, IGameCaptureBackend> windowsGraphicsCaptureFactory)
    {
        _visiblePixelsFactory = visiblePixelsFactory;
        _windowsGraphicsCaptureFactory = windowsGraphicsCaptureFactory;
    }

    public IGameCaptureBackend Create(CaptureBackendOptions options, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        TextWriter writer = log ?? TextWriter.Null;

        IGameCaptureBackend backend = CreateRequestedBackend(options.Backend, writer);
        CaptureBackendAvailability availability = backend.CheckAvailability();
        if (availability.Available)
        {
            writer.WriteLine($"Capture backend: {backend.Backend}");
            writer.WriteLine($"Capture backend status: {availability.Message}");
            return backend;
        }

        writer.WriteLine($"Capture backend: {backend.Backend}");
        writer.WriteLine($"Capture backend unavailable: {availability.FailureReason}. {availability.Message}");
        if (options.Backend == CaptureBackend.WindowsGraphicsCapture && options.AllowBackendFallback)
        {
            writer.WriteLine("Capture backend fallback enabled; falling back to VisiblePixels.");
            IGameCaptureBackend fallback = _visiblePixelsFactory(writer);
            CaptureBackendAvailability fallbackAvailability = fallback.CheckAvailability();
            if (!fallbackAvailability.Available)
            {
                throw new CaptureBackendException(
                    fallback.Backend,
                    fallbackAvailability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
                    fallbackAvailability.Message);
            }

            writer.WriteLine($"Capture backend: {fallback.Backend}");
            writer.WriteLine($"Capture backend status: {fallbackAvailability.Message}");
            return fallback;
        }

        throw new CaptureBackendException(
            backend.Backend,
            availability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
            availability.Message);
    }

    private IGameCaptureBackend CreateRequestedBackend(CaptureBackend backend, TextWriter log)
    {
        return backend switch
        {
            CaptureBackend.VisiblePixels => _visiblePixelsFactory(log),
            CaptureBackend.WindowsGraphicsCapture => _windowsGraphicsCaptureFactory(log),
            _ => throw new ArgumentException("Capture backend is not supported.", nameof(backend))
        };
    }
}
