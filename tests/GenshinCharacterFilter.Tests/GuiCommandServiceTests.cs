using GenshinCharacterFilter;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Gui;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class GuiCommandServiceTests
{
    [Fact]
    public void GetDefaultOcrInputPath_UsesOriginalCapturePath()
    {
        string path = GuiCommandService.GetDefaultOcrInputPath();

        Assert.Equal(Path.Combine("debug-captures", "capture-latest.png"), path);
        Assert.DoesNotContain("debug-ocr", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_UncheckedUsesLiveCapture()
    {
        string? path = GuiCommandService.ResolveDetectionLoopOcrInputPath(
            Path.Combine("debug-captures", "capture-latest.png"),
            useFixedImageForDetection: false);

        Assert.Null(path);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_CheckedUsesFixedImage()
    {
        string inputPath = Path.Combine("debug-captures", "capture-latest.png");

        string? path = GuiCommandService.ResolveDetectionLoopOcrInputPath(
            $"  {inputPath}  ",
            useFixedImageForDetection: true);

        Assert.Equal(inputPath, path);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_CheckedRequiresImagePath()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiCommandService.ResolveDetectionLoopOcrInputPath(
                " ",
                useFixedImageForDetection: true));

        Assert.Contains("fixed-image detection", exception.Message);
    }

    [Fact]
    public void EnsureGuardedRealAudioAllowsDetectionInput_RejectsFixedImageMode()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => GuiCommandService.EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection: true));

        Assert.Contains("does not allow fixed-image", exception.Message);
    }

    [Fact]
    public void EnsureGuardedRealAudioAllowsDetectionInput_AllowsLiveCaptureMode()
    {
        GuiCommandService.EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection: false);
    }

    [Fact]
    public void ParseGuiDetectionTuning_RunUntilStopUsesNullLoopCount()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: " ",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.True(options.RunUntilStop);
        Assert.Null(options.LoopCount);
        Assert.Equal("until stopped", options.FormatLoopCount(5));
    }

    [Fact]
    public void ParseGuiDetectionTuning_FixedLoopCountUsesNumber()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: false,
            loopCount: " 20 ",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.False(options.RunUntilStop);
        Assert.Equal(20, options.LoopCount);
        Assert.Equal("20", options.FormatLoopCount(20));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void ParseGuiDetectionTuning_RejectsInvalidLoopCount(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiDetectionTuningOptions.Parse(
                runUntilStop: false,
                loopCount: value,
                loopIntervalMs: "200",
                captureDelayMs: "100",
                matchThreshold: "2",
                missThreshold: "1"));

        Assert.Contains("Loop count", exception.Message);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsRealtimeDefaults()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.Equal(200, options.LoopIntervalMs);
        Assert.Equal(100, options.CaptureDelayMs);
        Assert.Equal(2, options.MatchThreshold);
        Assert.Equal(1, options.MissThreshold);
        Assert.False(options.SaveDebugImages);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsSaveDebugImages()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            saveDebugImages: true);

        Assert.True(options.SaveDebugImages);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsInputForegroundFallback()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            enableInputForegroundFallback: true);

        Assert.True(options.EnableInputForegroundFallback);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsCaptureBackend()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            captureBackend: CaptureBackend.WindowsGraphicsCapture,
            allowCaptureBackendFallback: true);

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, options.CaptureBackendOptions.Backend);
        Assert.True(options.CaptureBackendOptions.AllowBackendFallback);
    }

    [Theory]
    [InlineData("abc", "100", "2", "1", "Loop interval")]
    [InlineData("49", "100", "2", "1", "Loop interval")]
    [InlineData("200", "abc", "2", "1", "Capture delay")]
    [InlineData("200", "5001", "2", "1", "Capture delay")]
    [InlineData("200", "100", "0", "1", "Match threshold")]
    [InlineData("200", "100", "2", "0", "Miss threshold")]
    public void ParseGuiDetectionTuning_RejectsInvalidTimingAndThresholds(
        string loopIntervalMs,
        string captureDelayMs,
        string matchThreshold,
        string missThreshold,
        string expectedMessage)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiDetectionTuningOptions.Parse(
                runUntilStop: true,
                loopCount: "",
                loopIntervalMs: loopIntervalMs,
                captureDelayMs: captureDelayMs,
                matchThreshold: matchThreshold,
                missThreshold: missThreshold));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_RunUntilStopOverridesConfiguredLoopCount()
    {
        AppSettings settings = new();
        settings.Detection.LoopCount = 5;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "");

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Null(settings.Detection.LoopCount);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_NumberOverridesConfiguredLoopCount()
    {
        AppSettings settings = new();
        settings.Detection.LoopCount = 5;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: false,
            loopCount: "10",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "");

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Equal(10, settings.Detection.LoopCount);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_OverridesIntervalAndThresholds()
    {
        AppSettings settings = new();
        settings.Detection.LoopIntervalMs = 1000;
        settings.Detection.MatchThreshold = 3;
        settings.Detection.MissThreshold = 3;
        settings.Detection.SaveDebugImages = true;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "200",
            captureDelayMs: "",
            matchThreshold: "2",
            missThreshold: "1",
            saveDebugImages: false);

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Equal(200, settings.Detection.LoopIntervalMs);
        Assert.Equal(2, settings.Detection.MatchThreshold);
        Assert.Equal(1, settings.Detection.MissThreshold);
        Assert.False(settings.Detection.SaveDebugImages);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_PropagatesInputForegroundFallback()
    {
        AppSettings settings = new();
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            enableInputForegroundFallback: true);

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.True(settings.Detection.EnableInputForegroundFallback);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_PropagatesCaptureBackend()
    {
        AppSettings settings = new();
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            captureBackend: CaptureBackend.WindowsGraphicsCapture,
            allowCaptureBackendFallback: true);

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, settings.Capture.Backend);
        Assert.True(settings.Capture.AllowBackendFallback);
    }

    [Fact]
    public void ApplyGuiCaptureBackendOverride_PropagatesCurrentRunSelection()
    {
        AppSettings settings = new();

        GuiCommandService.ApplyGuiCaptureBackendOverride(settings, new CaptureBackendOptions
        {
            Backend = CaptureBackend.WindowsGraphicsCapture,
            AllowBackendFallback = true,
            CaptureTimeoutMs = 1500
        });

        Assert.Equal(CaptureBackend.WindowsGraphicsCapture, settings.Capture.Backend);
        Assert.True(settings.Capture.AllowBackendFallback);
        Assert.Equal(1500, settings.Capture.CaptureTimeoutMs);
    }

    [Theory]
    [InlineData("TesseractCli", OcrEngine.TesseractCli)]
    [InlineData("paddleocrlocal", OcrEngine.PaddleOcrLocal)]
    [InlineData("", OcrEngine.TesseractCli)]
    public void GuiOcrEngineSelection_ParsesSelection(string value, OcrEngine expected)
    {
        OcrEngine engine = GuiOcrEngineSelection.Parse(value);

        Assert.Equal(expected, engine);
    }

    [Fact]
    public void GuiOcrEngineSelection_RejectsInvalidSelection()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiOcrEngineSelection.Parse("Other"));

        Assert.Contains("OCR engine", exception.Message);
    }

    [Fact]
    public void ApplyGuiOcrEngineOverride_UsesSelectedEngine()
    {
        AppSettings settings = new();
        settings.Ocr.Engine = OcrEngine.TesseractCli;

        GuiCommandService.ApplyGuiOcrEngineOverride(settings, OcrEngine.PaddleOcrLocal);

        Assert.Equal(OcrEngine.PaddleOcrLocal, settings.Ocr.Engine);
    }

    [Fact]
    public void GuiOcrServiceCache_ReusesServiceForSameKey()
    {
        using GuiOcrServiceCache cache = new(engine => new FakeWarmOcrService(engine));
        GuiOcrBackendCacheKey key = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-a");

        IOcrService first = cache.Get(key);
        IOcrService second = cache.Get(key);

        Assert.Same(first, second);
    }

    [Fact]
    public void GuiOcrServiceCache_RecreatesWhenEngineChanges()
    {
        using GuiOcrServiceCache cache = new(engine => new FakeWarmOcrService(engine));
        GuiOcrBackendCacheKey paddleKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-a");
        GuiOcrBackendCacheKey tesseractKey = GuiOcrBackendCacheKey.From(
            OcrEngine.TesseractCli,
            null,
            null);

        IOcrService first = cache.Get(paddleKey);
        IOcrService second = cache.Get(tesseractKey);

        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task GuiOcrServiceCache_DifferentModelPathIsNotIncorrectlyReady()
    {
        using GuiOcrServiceCache cache = new(engine => new FakeWarmOcrService(engine));
        GuiOcrBackendCacheKey firstKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-a");
        GuiOcrBackendCacheKey secondKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-b",
            "runtime-a");

        await cache.WarmUpAsync(firstKey, new OcrOptions { OcrEngine = OcrEngine.PaddleOcrLocal }, CancellationToken.None);

        Assert.True(cache.IsWarm(firstKey));
        Assert.False(cache.IsWarm(secondKey));
    }

    [Fact]
    public async Task GuiOcrServiceCache_DifferentRuntimePathIsNotIncorrectlyReady()
    {
        using GuiOcrServiceCache cache = new(engine => new FakeWarmOcrService(engine));
        GuiOcrBackendCacheKey firstKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-a");
        GuiOcrBackendCacheKey secondKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-b");

        await cache.WarmUpAsync(firstKey, new OcrOptions { OcrEngine = OcrEngine.PaddleOcrLocal }, CancellationToken.None);

        Assert.True(cache.IsWarm(firstKey));
        Assert.False(cache.IsWarm(secondKey));
    }

    [Fact]
    public async Task GuiOcrServiceCache_SwitchingBackToPreviousKeyReusesWarmService()
    {
        using GuiOcrServiceCache cache = new(engine => new FakeWarmOcrService(engine));
        GuiOcrBackendCacheKey firstKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-a",
            "runtime-a");
        GuiOcrBackendCacheKey secondKey = GuiOcrBackendCacheKey.From(
            OcrEngine.PaddleOcrLocal,
            "models-b",
            "runtime-a");

        IOcrService first = cache.Get(firstKey);
        await cache.WarmUpAsync(firstKey, new OcrOptions { OcrEngine = OcrEngine.PaddleOcrLocal }, CancellationToken.None);
        _ = cache.Get(secondKey);
        IOcrService firstAgain = cache.Get(firstKey);

        Assert.Same(first, firstAgain);
        Assert.True(cache.IsWarm(firstKey));
    }

    [Fact]
    public void ResolveGuiCaptureDelayMs_UsesGuiOverrideWhenSpecified()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "100",
            matchThreshold: "",
            missThreshold: "");

        int captureDelayMs = GuiCommandService.ResolveGuiCaptureDelayMs(500, options);

        Assert.Equal(100, captureDelayMs);
    }

    private sealed class FakeWarmOcrService : IOcrService, IOcrBackendWarmup, IDisposable
    {
        public FakeWarmOcrService(OcrEngine engine)
        {
            Engine = engine;
        }

        public OcrEngine Engine { get; }

        public bool IsWarm { get; private set; }

        public bool Disposed { get; private set; }

        public Task WarmUpAsync(OcrOptions options, CancellationToken cancellationToken)
        {
            IsWarm = true;
            return Task.CompletedTask;
        }

        public Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
        {
            return Task.FromResult(new OcrResult(string.Empty, Engine.ToString(), options.InputImagePath ?? string.Empty));
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    [Fact]
    public void ManualForegroundFallbackPolicy_PromptsForLiveDetectionMinimizedFailure()
    {
        WindowCaptureException exception = WindowCaptureException.TargetWindowMinimizedCannotRestore("YuanShen");

        bool shouldPrompt = GuiManualForegroundFallbackPolicy.ShouldPromptForDetection(
            exception,
            useFixedImageForDetection: false);

        Assert.True(shouldPrompt);
    }

    [Fact]
    public void ManualForegroundFallbackPolicy_DoesNotPromptForFixedImageMode()
    {
        WindowCaptureException exception = WindowCaptureException.TargetWindowMinimizedCannotRestore("YuanShen");

        bool shouldPrompt = GuiManualForegroundFallbackPolicy.ShouldPromptForDetection(
            exception,
            useFixedImageForDetection: true);

        Assert.False(shouldPrompt);
    }

    [Fact]
    public void ManualForegroundFallbackPolicy_DoesNotPromptForOtherFailures()
    {
        bool shouldPrompt = GuiManualForegroundFallbackPolicy.ShouldPromptForDetection(
            new InvalidOperationException("config failed"),
            useFixedImageForDetection: false);

        Assert.False(shouldPrompt);
    }

    [Fact]
    public void ManualForegroundFallbackFlow_DelaysRestoreUntilOperationCompletes()
    {
        Assert.False(GuiManualForegroundFallbackFlow.ShouldRestoreAfterSessionReady);
        Assert.True(GuiManualForegroundFallbackFlow.ShouldRestoreAfterOperationCompleted);
    }

    [Fact]
    public void ManualForegroundFallbackFlow_RestoresWhenStartupDoesNotContinue()
    {
        Assert.True(GuiManualForegroundFallbackFlow.ShouldRestoreAfterSessionFailure);
        Assert.True(GuiManualForegroundFallbackFlow.ShouldRestoreAfterUserCancel);
    }

    [Fact]
    public void ForegroundActivationPolicy_SuccessSkipsFallbacks()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Succeeded(TargetWindowActivationMethod.Win32);

        Assert.False(GuiForegroundActivationPolicy.ShouldTryInputFallback(result, inputFallbackEnabled: true));
        Assert.False(GuiForegroundActivationPolicy.ShouldUseManualFallback(result));
        Assert.False(GuiForegroundActivationPolicy.ShouldFailImmediately(result));
    }

    [Fact]
    public void ForegroundActivationPolicy_FailedActivationCanTryInputFallbackWhenEnabled()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Failed(
            TargetWindowActivationFailureReason.ActivationDenied,
            "Windows denied foreground activation.");

        Assert.True(GuiForegroundActivationPolicy.ShouldTryInputFallback(result, inputFallbackEnabled: true));
        Assert.False(GuiForegroundActivationPolicy.ShouldTryInputFallback(result, inputFallbackEnabled: false));
        Assert.True(GuiForegroundActivationPolicy.ShouldUseManualFallback(result));
    }

    [Fact]
    public void ForegroundActivationPolicy_InputFallbackFailureStillUsesManualFallback()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Failed(
            TargetWindowActivationFailureReason.StillMinimized,
            "Target stayed minimized.",
            inputFallbackAttempted: true);

        Assert.True(GuiForegroundActivationPolicy.ShouldUseManualFallback(result));
        Assert.False(GuiForegroundActivationPolicy.ShouldFailImmediately(result));
    }

    [Fact]
    public void ForegroundActivationPolicy_TargetNotFoundFailsImmediately()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Failed(
            TargetWindowActivationFailureReason.TargetNotFound,
            "No running process named 'YuanShen' was found.");

        Assert.False(GuiForegroundActivationPolicy.ShouldTryInputFallback(result, inputFallbackEnabled: true));
        Assert.False(GuiForegroundActivationPolicy.ShouldUseManualFallback(result));
        Assert.True(GuiForegroundActivationPolicy.ShouldFailImmediately(result));
    }

    [Fact]
    public void ForegroundActivationPolicy_ForegroundMismatchUsesManualFallback()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Failed(
            TargetWindowActivationFailureReason.ForegroundMismatch,
            "Foreground window is not target.");

        Assert.True(GuiForegroundActivationPolicy.ShouldUseManualFallback(result));
    }

    [Fact]
    public void ForegroundActivationPolicy_VisiblePixelsLiveCaptureUsesForegroundStartup()
    {
        Assert.True(GuiForegroundActivationPolicy.ShouldUseForegroundStartup(
            CaptureBackend.VisiblePixels,
            useFixedImageForDetection: false));
    }

    [Fact]
    public void ForegroundActivationPolicy_WindowsGraphicsCaptureBypassesForegroundStartup()
    {
        Assert.False(GuiForegroundActivationPolicy.ShouldUseForegroundStartup(
            CaptureBackend.WindowsGraphicsCapture,
            useFixedImageForDetection: false));
    }

    [Fact]
    public void ForegroundActivationPolicy_FixedImageBypassesForegroundStartup()
    {
        Assert.False(GuiForegroundActivationPolicy.ShouldUseForegroundStartup(
            CaptureBackend.VisiblePixels,
            useFixedImageForDetection: true));
    }
}
