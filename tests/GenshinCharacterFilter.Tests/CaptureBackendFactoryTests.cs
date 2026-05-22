using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class CaptureBackendFactoryTests
{
    [Fact]
    public void Create_SelectedVisiblePixelsCreatesVisibleBackend()
    {
        CaptureBackendFactory factory = CreateFactory(
            visibleAvailable: true,
            wgcAvailable: false);

        IGameCaptureBackend backend = factory.Create(new CaptureBackendOptions
        {
            Backend = CaptureBackend.VisiblePixels
        });

        Assert.Equal(CaptureBackend.VisiblePixels, backend.Backend);
    }

    [Fact]
    public void Create_SelectedWindowsGraphicsCaptureCreatesWgcWhenAvailable()
    {
        CaptureBackendFactory factory = CreateFactory(
            visibleAvailable: true,
            wgcAvailable: true);

        IGameCaptureBackend backend = factory.Create(new CaptureBackendOptions
        {
            Backend = CaptureBackend.WindowsGraphicsCapture
        });

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, backend.Backend);
    }

    [Fact]
    public void Create_WgcUnavailableWithFallbackAllowedFallsBackToVisiblePixels()
    {
        StringWriter log = new();
        CaptureBackendFactory factory = CreateFactory(
            visibleAvailable: true,
            wgcAvailable: false);

        IGameCaptureBackend backend = factory.Create(new CaptureBackendOptions
        {
            Backend = CaptureBackend.WindowsGraphicsCapture,
            AllowBackendFallback = true
        }, log);

        Assert.Equal(CaptureBackend.VisiblePixels, backend.Backend);
        Assert.Contains("Requested capture backend: WindowsGraphicsCapture", log.ToString());
        Assert.Contains("Fallback reason: BackendUnavailable. fake unavailable", log.ToString());
        Assert.Contains("Actual capture backend: VisiblePixels", log.ToString());
        Assert.Contains("falling back to VisiblePixels", log.ToString());
    }

    [Fact]
    public void Create_WgcUnavailableWithFallbackDisabledThrowsClearError()
    {
        StringWriter log = new();
        CaptureBackendFactory factory = CreateFactory(
            visibleAvailable: true,
            wgcAvailable: false);

        CaptureBackendException exception = Assert.Throws<CaptureBackendException>(() => factory.Create(new CaptureBackendOptions
        {
            Backend = CaptureBackend.WindowsGraphicsCapture,
            AllowBackendFallback = false
        }, log));

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, exception.Backend);
        Assert.Equal(CaptureBackendFailureReason.BackendUnavailable, exception.Reason);
        Assert.Contains("fake unavailable", exception.Message);
        Assert.Contains("Requested capture backend: WindowsGraphicsCapture", log.ToString());
        Assert.Contains("Actual capture backend: (none)", log.ToString());
        Assert.Contains("Backend fallback disabled.", log.ToString());
    }

    private static CaptureBackendFactory CreateFactory(bool visibleAvailable, bool wgcAvailable)
    {
        return new CaptureBackendFactory(
            _ => new FakeCaptureBackend(CaptureBackend.VisiblePixels, visibleAvailable),
            _ => new FakeCaptureBackend(CaptureBackend.WindowsGraphicsCapture, wgcAvailable));
    }

    private sealed class FakeCaptureBackend : IGameCaptureBackend
    {
        private readonly bool _available;

        public FakeCaptureBackend(CaptureBackend backend, bool available)
        {
            Backend = backend;
            _available = available;
        }

        public CaptureBackend Backend { get; }

        public string StatusLabel => _available ? "Ready" : "Unavailable";

        public CaptureBackendAvailability CheckAvailability()
        {
            return _available
                ? CaptureBackendAvailability.Ready("fake ready")
                : CaptureBackendAvailability.Unavailable(CaptureBackendFailureReason.BackendUnavailable, "fake unavailable");
        }

        public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult("fake.png");
        }

        public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
        {
            return new FakeSession();
        }
    }

    private sealed class FakeSession : IGameWindowCaptureSession
    {
        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string> CaptureAsync(CancellationToken cancellationToken) => Task.FromResult("fake.png");

        public Task<WindowCaptureFrameInfo> GetFrameInfoAsync(CancellationToken cancellationToken) => Task.FromResult(new WindowCaptureFrameInfo(10, 10));

        public Task<string> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken) => Task.FromResult("fake-region.png");

        public void Dispose()
        {
        }
    }
}
