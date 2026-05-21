using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;
using GenshinCharacterFilter.Audio;
using System.Drawing;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionDryRunLoopTests
{
    [Fact]
    public async Task RunAsync_LoopCountTwoCallsFakeOcrTwice()
    {
        using TempFile input = TempFile.Create();
        FakeOcrService ocr = new(["target", "target"]);
        FakeSpeakerMatcher matcher = new();
        FakeWindowCapture capture = new();
        StringWriter log = new();
        DetectionDryRunLoop loop = new(ocr, matcher, capture, log: log);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        Assert.Equal(2, ocr.CallCount);
        Assert.Equal(2, matcher.CallCount);
    }

    [Fact]
    public async Task RunAsync_FixedImageModeDoesNotCallCapture()
    {
        using TempFile input = TempFile.Create();
        FakeWindowCapture capture = new();
        DetectionDryRunLoop loop = new(new FakeOcrService(["target", "target"]), new FakeSpeakerMatcher(), capture);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        Assert.Equal(0, capture.CallCount);
    }

    [Fact]
    public async Task RunAsync_LiveModeInitializesSessionOnceAndReusesIt()
    {
        using TempFile firstCapture = TempFile.Create();
        using TempFile secondCapture = TempFile.Create();
        FakeSessionWindowCapture capture = new([firstCapture.Path, secondCapture.Path]);
        DetectionDryRunLoop loop = new(new FakeOcrService(["target", "target"]), new FakeSpeakerMatcher(), capture);

        await loop.RunAsync(CreateLiveOptions(), CancellationToken.None);

        Assert.Equal(1, capture.SessionCreateCount);
        Assert.Equal(1, capture.Session.InitializeCount);
        Assert.Equal(2, capture.Session.CaptureCount);
        Assert.Equal(0, capture.OneShotCaptureCount);
        Assert.False(capture.LastSessionOptions!.SaveDebugImage);
    }

    [Fact]
    public async Task RunAsync_LiveModePassesDebugSaveOptionWhenEnabled()
    {
        using TempFile firstCapture = TempFile.Create();
        FakeSessionWindowCapture capture = new([firstCapture.Path]);
        DetectionDryRunOptions options = CreateLiveOptions();
        options.LoopCount = 1;
        options.SaveDebugImages = true;
        DetectionDryRunLoop loop = new(new FakeOcrService(["target"]), new FakeSpeakerMatcher(), capture);

        await loop.RunAsync(options, CancellationToken.None);

        Assert.True(capture.LastSessionOptions!.SaveDebugImage);
    }

    [Fact]
    public async Task RunAsync_LiveModeWithOcrRegionUsesRegionOnlyCapture()
    {
        using TempFile regionCapture = TempFile.Create();
        FakeSessionWindowCapture capture = new([regionCapture.Path]);
        DetectionDryRunOptions options = CreateLiveOptions();
        options.LoopCount = 1;
        options.OcrRegion = new OcrRegion(1, 2, 3, 4);
        StringWriter log = new();
        DetectionDryRunLoop loop = new(new FakeOcrService(["target"]), new FakeSpeakerMatcher(), capture, log: log);

        await loop.RunAsync(options, CancellationToken.None);

        Assert.Equal(1, capture.Session.RegionCaptureCount);
        Assert.Equal(0, capture.Session.CaptureCount);
        Assert.Equal(new CaptureRegion(1, 2, 3, 4), capture.Session.LastRegion);
        Assert.Contains("Capture mode: process-region-only", log.ToString());
    }

    [Fact]
    public async Task RunAsync_PreinitializedForegroundSessionUsesForegroundRegionMode()
    {
        using TempFile regionCapture = TempFile.Create();
        FakeGameWindowCaptureSession session = new([regionCapture.Path], captureModePrefix: "foreground");
        PreinitializedGameWindowCapture capture = new(session);
        DetectionDryRunOptions options = CreateLiveOptions();
        options.LoopCount = 1;
        options.OcrRegion = new OcrRegion(1, 2, 3, 4);
        StringWriter log = new();
        DetectionDryRunLoop loop = new(new FakeOcrService(["target"]), new FakeSpeakerMatcher(), capture, log: log);

        await loop.RunAsync(options, CancellationToken.None);

        Assert.Equal(1, session.InitializeCount);
        Assert.Equal(1, session.RegionCaptureCount);
        Assert.Equal(0, session.CaptureCount);
        Assert.Contains("Capture mode: foreground-region-only", log.ToString());
        Assert.DoesNotContain("Reacquiring target window", log.ToString());
    }

    [Fact]
    public async Task RunAsync_LiveModeFallsBackToFullWindowWhenRegionCaptureFails()
    {
        using TempFile regionCapture = TempFile.Create();
        using TempFile fullCapture = TempFile.Create();
        FakeSessionWindowCapture capture = new([regionCapture.Path, fullCapture.Path]);
        capture.Session.ThrowRegionCapture = true;
        DetectionDryRunOptions options = CreateLiveOptions();
        options.LoopCount = 1;
        options.OcrRegion = new OcrRegion(1, 2, 3, 4);
        StringWriter log = new();
        DetectionDryRunLoop loop = new(new FakeOcrService(["target"]), new FakeSpeakerMatcher(), capture, log: log);

        await loop.RunAsync(options, CancellationToken.None);

        Assert.Equal(1, capture.Session.RegionCaptureCount);
        Assert.Equal(1, capture.Session.CaptureCount);
        string output = log.ToString();
        Assert.Contains("Capture mode: full-window fallback", output);
        Assert.Contains("Region-only capture fallback reason:", output);
    }

    [Fact]
    public async Task RunAsync_ReportsStableStateChangeWhenThresholdIsReached()
    {
        using TempFile input = TempFile.Create();
        StringWriter log = new();
        DetectionDryRunLoop loop = new(
            new FakeOcrService(["target", "target"]),
            new FakeSpeakerMatcher(),
            new FakeWindowCapture(),
            log: log);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        string output = log.ToString();
        Assert.Contains("Raw matched: True", output);
        Assert.Contains("Stable matched: True", output);
        Assert.Contains("Consecutive match count: 2", output);
        Assert.Contains("Iteration timing:", output);
        Assert.Contains("  Capture:", output);
        Assert.Contains("  OCR:", output);
        Assert.Contains("  Match:", output);
        Assert.Contains("  Audio:", output);
        Assert.Contains("  Total:", output);
        Assert.Contains("Stable state changed: NotMatched -> Matched(Wanderer)", output);
    }

    [Fact]
    public async Task RunAsync_WithSimulatedAudioCoordinatorReportsMuteAndShutdownRestore()
    {
        using TempFile input = TempFile.Create();
        FakeAudioMuteService audio = new();
        StringWriter log = new();
        DetectionDryRunLoop loop = new(
            new FakeOcrService(["target", "target"]),
            new FakeSpeakerMatcher(),
            new FakeWindowCapture(),
            audioCoordinator: new SimulatedDetectionAudioCoordinator(audio),
            log: log);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        string output = log.ToString();
        Assert.Contains("Simulated audio action: mute", output);
        Assert.Contains("Simulated audio shutdown action: restore", output);
        Assert.Equal(1, audio.MuteCalls);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Fact]
    public async Task RunAsync_WhenEnabledSavesOcrFailureSample()
    {
        using TempFile input = TempFile.Create();
        string outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            DetectionDryRunOptions options = CreateOptions(input.Path);
            options.LoopCount = 1;
            options.SaveOcrFailureSamples = true;
            DetectionDryRunLoop loop = new(
                new FakeOcrService(["miss"]),
                new FakeSpeakerMatcher(),
                new FakeWindowCapture(),
                failureSampleSaver: new OcrFailureSampleSaver(outputDirectory));

            await loop.RunAsync(options, CancellationToken.None);

            Assert.Single(Directory.GetFiles(outputDirectory, "*.png"));
            Assert.Single(Directory.GetFiles(outputDirectory, "*.json"));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static DetectionDryRunOptions CreateOptions(string inputPath)
    {
        return new DetectionDryRunOptions
        {
            OcrInputPath = inputPath,
            LoopCount = 2,
            LoopIntervalMs = DetectionDryRunOptions.MinLoopIntervalMs,
            TargetSpeakers = ["Wanderer"],
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = 2,
                MissThreshold = 2
            }
        };
    }

    private static DetectionDryRunOptions CreateLiveOptions()
    {
        return new DetectionDryRunOptions
        {
            TargetProcessName = "fake",
            LoopCount = 2,
            LoopIntervalMs = DetectionDryRunOptions.MinLoopIntervalMs,
            TargetSpeakers = ["Wanderer"],
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = 2,
                MissThreshold = 2
            }
        };
    }

    private sealed class FakeOcrService : IOcrService
    {
        private readonly Queue<string> _rawTexts;

        public FakeOcrService(IEnumerable<string> rawTexts)
        {
            _rawTexts = new Queue<string>(rawTexts);
        }

        public int CallCount { get; private set; }

        public Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            string rawText = _rawTexts.Count > 0 ? _rawTexts.Dequeue() : string.Empty;
            return Task.FromResult(new OcrResult(rawText, "FakeOcr", options.InputImagePath!));
        }
    }

    private sealed class FakeSpeakerMatcher : ISpeakerMatcher
    {
        public int CallCount { get; private set; }

        public SpeakerMatchResult Match(string? rawText, SpeakerMatcherOptions options)
        {
            CallCount++;
            bool matched = string.Equals(rawText, "target", StringComparison.OrdinalIgnoreCase);
            return matched
                ? new SpeakerMatchResult(true, "Wanderer", rawText ?? string.Empty, rawText ?? string.Empty)
                : new SpeakerMatchResult(false, null, rawText ?? string.Empty, rawText ?? string.Empty);
        }
    }

    private sealed class FakeWindowCapture : IGameWindowCapture
    {
        public int CallCount { get; private set; }

        public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult("capture.png");
        }
    }

    private sealed class FakeSessionWindowCapture : IGameWindowCapture, IGameWindowCaptureSessionFactory
    {
        public FakeSessionWindowCapture(IEnumerable<string> paths)
        {
            Session = new FakeGameWindowCaptureSession(paths);
        }

        public int OneShotCaptureCount { get; private set; }

        public int SessionCreateCount { get; private set; }

        public WindowCaptureOptions? LastSessionOptions { get; private set; }

        public FakeGameWindowCaptureSession Session { get; }

        public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
        {
            OneShotCaptureCount++;
            return Task.FromResult("one-shot.png");
        }

        public IGameWindowCaptureSession CreateSession(WindowCaptureOptions options)
        {
            SessionCreateCount++;
            LastSessionOptions = options;
            return Session;
        }
    }

    private sealed class FakeGameWindowCaptureSession : IGameWindowCaptureSession, IGameWindowCaptureSessionMetadata
    {
        private readonly Queue<string> _paths;

        public FakeGameWindowCaptureSession(IEnumerable<string> paths, string captureModePrefix = "process")
        {
            _paths = new Queue<string>(paths);
            CaptureModePrefix = captureModePrefix;
        }

        public string CaptureModePrefix { get; }

        public int InitializeCount { get; private set; }

        public int CaptureCount { get; private set; }

        public int RegionCaptureCount { get; private set; }

        public CaptureRegion? LastRegion { get; private set; }

        public bool ThrowRegionCapture { get; set; }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCount++;
            return Task.CompletedTask;
        }

        public Task<string> CaptureAsync(CancellationToken cancellationToken)
        {
            CaptureCount++;
            return Task.FromResult(_paths.Count > 0 ? _paths.Dequeue() : "capture.png");
        }

        public Task<WindowCaptureFrameInfo> GetFrameInfoAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new WindowCaptureFrameInfo(20, 20));
        }

        public Task<string> CaptureRegionAsync(CaptureRegion region, CancellationToken cancellationToken)
        {
            RegionCaptureCount++;
            LastRegion = region;
            if (ThrowRegionCapture)
            {
                throw new WindowCaptureException("fake region capture failed");
            }

            return Task.FromResult(_paths.Count > 0 ? _paths.Dequeue() : "region-capture.png");
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeAudioMuteService : IAudioMuteService
    {
        public int MuteCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public Task MuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MuteCalls++;
            return Task.CompletedTask;
        }

        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RestoreCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFile Create()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            using Bitmap bitmap = new(20, 20);
            bitmap.SetPixel(0, 0, Color.Black);
            bitmap.Save(path);
            return new TempFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
