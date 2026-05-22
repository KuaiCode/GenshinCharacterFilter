namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Creates configured live capture backend instances and applies explicit fallback policy.
/// </summary>
public sealed class CaptureBackendFactory
{
    private readonly Func<CaptureBackendOptions, TextWriter?, IGameCaptureBackend> _visiblePixelsFactory;
    private readonly Func<CaptureBackendOptions, TextWriter?, IGameCaptureBackend> _windowsGraphicsCaptureFactory;

    public CaptureBackendFactory()
        : this(
            (_, log) => new VisiblePixelsCaptureBackend(log),
            (options, log) => new WindowsGraphicsCaptureBackend(options.CaptureTimeoutMs, log))
    {
    }

    public CaptureBackendFactory(
        Func<TextWriter?, IGameCaptureBackend> visiblePixelsFactory,
        Func<TextWriter?, IGameCaptureBackend> windowsGraphicsCaptureFactory)
        : this(
            (_, log) => visiblePixelsFactory(log),
            (_, log) => windowsGraphicsCaptureFactory(log))
    {
    }

    public CaptureBackendFactory(
        Func<CaptureBackendOptions, TextWriter?, IGameCaptureBackend> visiblePixelsFactory,
        Func<CaptureBackendOptions, TextWriter?, IGameCaptureBackend> windowsGraphicsCaptureFactory)
    {
        _visiblePixelsFactory = visiblePixelsFactory;
        _windowsGraphicsCaptureFactory = windowsGraphicsCaptureFactory;
    }

    public IGameCaptureBackend Create(CaptureBackendOptions options, TextWriter? log = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        TextWriter writer = log ?? TextWriter.Null;

        writer.WriteLine($"Requested capture backend: {options.Backend}");
        IGameCaptureBackend backend = CreateRequestedBackend(options, writer);
        CaptureBackendAvailability availability = backend.CheckAvailability();
        if (availability.Available)
        {
            writer.WriteLine($"Actual capture backend: {backend.Backend}");
            writer.WriteLine($"Capture backend: {backend.Backend}");
            writer.WriteLine($"Capture backend status: {availability.Message}");
            return backend;
        }

        writer.WriteLine($"Capture backend: {backend.Backend}");
        writer.WriteLine($"Capture backend unavailable: {availability.FailureReason}. {availability.Message}");
        if (backend.Backend == CaptureBackend.WindowsGraphicsCapture)
        {
            writer.WriteLine($"WGC failed: {availability.FailureReason}. {availability.Message}");
        }

        if (options.Backend == CaptureBackend.WindowsGraphicsCapture && options.AllowBackendFallback)
        {
            writer.WriteLine($"Fallback reason: {availability.FailureReason}. {availability.Message}");
            writer.WriteLine("Falling back to VisiblePixels.");
            writer.WriteLine("Capture backend fallback enabled; falling back to VisiblePixels.");
            IGameCaptureBackend fallback = _visiblePixelsFactory(options, writer);
            CaptureBackendAvailability fallbackAvailability = fallback.CheckAvailability();
            if (!fallbackAvailability.Available)
            {
                throw new CaptureBackendException(
                    fallback.Backend,
                    fallbackAvailability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
                    fallbackAvailability.Message);
            }

            writer.WriteLine($"Actual capture backend: {fallback.Backend}");
            writer.WriteLine($"Capture backend: {fallback.Backend}");
            writer.WriteLine($"Capture backend status: {fallbackAvailability.Message}");
            return fallback;
        }

        writer.WriteLine("Actual capture backend: (none)");
        if (options.Backend == CaptureBackend.WindowsGraphicsCapture)
        {
            writer.WriteLine("Backend fallback disabled.");
        }

        throw new CaptureBackendException(
            backend.Backend,
            availability.FailureReason ?? CaptureBackendFailureReason.BackendUnavailable,
            availability.Message);
    }

    private IGameCaptureBackend CreateRequestedBackend(CaptureBackendOptions options, TextWriter log)
    {
        return options.Backend switch
        {
            CaptureBackend.VisiblePixels => _visiblePixelsFactory(options, log),
            CaptureBackend.WindowsGraphicsCapture => _windowsGraphicsCaptureFactory(options, log),
            _ => throw new ArgumentException("Capture backend is not supported.", nameof(options))
        };
    }
}
