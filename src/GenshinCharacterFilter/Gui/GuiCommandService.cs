using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Calibration;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Runs existing application commands for the minimal WinForms control panel.
/// </summary>
public sealed class GuiCommandService
{
    private readonly AppSettingsLoader _settingsLoader = new();
    private readonly GuiOcrServiceCache _ocrServices = new();

    private enum GuiLiveCaptureStartupMode
    {
        AutomaticTargetWindow,
        ManualForegroundWindow
    }

    public void ValidateConfig(string? configPath, TextWriter log)
    {
        AppCommandLineOptions options = BuildOptions(configPath, "--validate-config");
        AppSettings settings = LoadMergedSettings(options);
        RuntimePreflightResult preflightResult = new AppPreflightValidator().Validate(
            settings,
            options,
            AppPreflightMode.ValidateConfig);

        if (!preflightResult.Passed)
        {
            log.WriteLine("Validation failed.");
            WritePreflightIssues(preflightResult, log);
            return;
        }

        log.WriteLine("Validation passed.");
    }

    public void PrintEffectiveConfig(string? configPath, TextWriter log)
    {
        PrintEffectiveConfig(configPath, ocrEngineOverride: null, log);
    }

    public void PrintEffectiveConfig(string? configPath, OcrEngine? ocrEngineOverride, TextWriter log)
    {
        AppCommandLineOptions options = BuildOptions(configPath, "--print-effective-config");
        AppSettings settings = LoadMergedSettings(options);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
        new EffectiveConfigPrinter().Print(settings, options, log);
    }

    public async Task CalibrateOcrRegionAsync(string? configPath, TextWriter log, CancellationToken cancellationToken)
    {
        CalibrationCommandContext context = BuildCalibrationContext(configPath);

        EnsurePreflightPassed(context.Settings, context.Options);
        log.WriteLine("Starting OCR region calibration.");

        WindowsOcrRegionCalibrator calibrator = new(
            new WindowsGameWindowCapture(log),
            log);

        OcrRegionCalibrationResult result = await calibrator.CalibrateAsync(context.CalibrationOptions, cancellationToken);
        WriteCalibrationResult(context.CalibrationOptions, result, log);
    }

    public async Task CalibrateOcrRegionFromForegroundWindowAsync(
        string? configPath,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        CalibrationCommandContext context = BuildCalibrationContext(configPath);

        EnsurePreflightPassed(context.Settings, context.Options);
        log.WriteLine("Starting manual foreground OCR region calibration.");

        WindowsGameWindowCapture capture = new(log);
        string screenshotPath = await capture.CaptureForegroundWindowAsync(
            context.CalibrationOptions.ToWindowCaptureOptions(),
            cancellationToken);

        WindowsOcrRegionCalibrator calibrator = new(capture, log);
        OcrRegionCalibrationResult result = calibrator.CalibrateFromScreenshot(
            context.CalibrationOptions,
            screenshotPath,
            cancellationToken);
        WriteCalibrationResult(context.CalibrationOptions, result, log);
    }

    public async Task<TargetWindowActivationResult> TryActivateTargetWindowAsync(
        string? configPath,
        int delayMs,
        bool enableInputForegroundFallback,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        AppSettings settings = LoadSettings(configPath);
        settings.Validate();
        WindowsTargetWindowActivator activator = new(log);
        return await activator.TryActivateTargetWindowAsync(
            settings.TargetProcessName,
            delayMs,
            new TargetWindowActivationOptions
            {
                EnableInputForegroundFallback = enableInputForegroundFallback,
                VerifyForegroundProcess = true
            },
            cancellationToken);
    }

    private static void WriteCalibrationResult(
        OcrRegionCalibrationOptions calibrationOptions,
        OcrRegionCalibrationResult result,
        TextWriter log)
    {
        log.WriteLine($"Calibration output: {calibrationOptions.GetCalibrationOutputPath()}");
        log.WriteLine($"Source image: {result.SourceImageWidth}x{result.SourceImageHeight}");
        log.WriteLine($"Region pixels: {result.RegionPixels}");
        log.WriteLine(
            $"Region ratio: x={result.RegionRatio.X:F6}, y={result.RegionRatio.Y:F6}, width={result.RegionRatio.Width:F6}, height={result.RegionRatio.Height:F6}");
    }

    public async Task OcrOnceAsync(
        string? configPath,
        string ocrInputPath,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        await OcrOnceAsync(configPath, ocrInputPath, ocrEngineOverride: null, log, cancellationToken);
    }

    public async Task OcrOnceAsync(
        string? configPath,
        string ocrInputPath,
        OcrEngine? ocrEngineOverride,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        AppCommandLineOptions options = BuildOptions(configPath, "--ocr-once", "--ocr-input", ocrInputPath);
        AppSettings settings = LoadMergedSettings(options);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
        EnsurePreflightPassed(settings, options);

        log.WriteLine("OCR mode; this run does not control real system audio.");
        OcrResult result = await ExtractOcrAsync(settings, options, log, cancellationToken);
        log.WriteLine($"OCR engine: {result.EngineName}");
        log.WriteLine($"OCR input path: {result.InputImagePath}");
        log.WriteLine("OCR raw text:");
        log.WriteLine(result.RawText);
    }

    public async Task<GuiOcrWarmupResult> WarmUpOcrBackendAsync(
        string? configPath,
        OcrEngine? ocrEngineOverride,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        AppCommandLineOptions options = BuildOptions(configPath);
        AppSettings settings = LoadMergedSettings(options);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);

        OcrOptions ocrOptions = BuildOcrOptionsForSettings(settings);
        log.WriteLine($"OCR backend warmup requested: {settings.Ocr.Engine}");
        GuiOcrBackendCacheKey backendKey = GetOcrBackendCacheKey(settings);
        log.WriteLine($"OCR backend initialized before warmup: {IsOcrBackendWarm(settings)}");
        if (settings.Ocr.Engine == OcrEngine.PaddleOcrLocal)
        {
            log.WriteLine($"Paddle model path: {FormatOptionalPath(settings.Ocr.PaddleModelDirectory)}");
            log.WriteLine($"Paddle runtime path: {FormatOptionalPath(settings.Ocr.PaddleRuntimeDirectory)}");
        }

        GuiOcrWarmupResult result = await _ocrServices.WarmUpAsync(
            backendKey,
            ocrOptions,
            cancellationToken);
        log.WriteLine($"OCR backend warmed up: {result.Engine}, elapsed: {result.ElapsedMs} ms");
        return result;
    }

    public bool IsOcrBackendWarm(OcrEngine engine)
    {
        return _ocrServices.IsWarm(GuiOcrBackendCacheKey.From(engine, null, null));
    }

    public bool IsOcrBackendWarm(string? configPath, OcrEngine? ocrEngineOverride)
    {
        AppSettings settings = LoadSettings(configPath);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
        return IsOcrBackendWarm(settings);
    }

    public async Task RunDetectionLoopAsync(
        string? configPath,
        string? ocrInputPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        bool simulateAudio,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        await RunDetectionLoopAsync(
            configPath,
            ocrInputPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride: null,
            simulateAudio,
            log,
            cancellationToken);
    }

    public async Task RunDetectionLoopAsync(
        string? configPath,
        string? ocrInputPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        bool simulateAudio,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted = null)
    {
        await RunDetectionLoopCoreAsync(
            configPath,
            ocrInputPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride,
            simulateAudio,
            GuiLiveCaptureStartupMode.AutomaticTargetWindow,
            afterForegroundSessionReady: null,
            log,
            cancellationToken,
            iterationCompleted);
    }

    public async Task RunDetectionLoopFromForegroundWindowAsync(
        string? configPath,
        string? ocrInputPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        bool simulateAudio,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        await RunDetectionLoopFromForegroundWindowAsync(
            configPath,
            ocrInputPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride: null,
            simulateAudio,
            afterForegroundSessionReady,
            log,
            cancellationToken);
    }

    public async Task RunDetectionLoopFromForegroundWindowAsync(
        string? configPath,
        string? ocrInputPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        bool simulateAudio,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted = null)
    {
        await RunDetectionLoopCoreAsync(
            configPath,
            ocrInputPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride,
            simulateAudio,
            GuiLiveCaptureStartupMode.ManualForegroundWindow,
            afterForegroundSessionReady,
            log,
            cancellationToken,
            iterationCompleted);
    }

    private async Task RunDetectionLoopCoreAsync(
        string? configPath,
        string? ocrInputPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        bool simulateAudio,
        GuiLiveCaptureStartupMode liveCaptureStartupMode,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted)
    {
        List<string> arguments = ["--detect-loop"];
        if (simulateAudio)
        {
            arguments.Add("--simulate-audio-from-detection");
        }

        string? detectionOcrInputPath = ResolveDetectionLoopOcrInputPath(ocrInputPath, useFixedImageForDetection);
        if (!string.IsNullOrWhiteSpace(detectionOcrInputPath))
        {
            arguments.Add("--ocr-input");
            arguments.Add(detectionOcrInputPath);
        }

        AppCommandLineOptions options = BuildOptions(configPath, [.. arguments]);
        AppSettings settings = LoadMergedSettings(options);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
        ApplyGuiDetectionTuningOverrides(settings, tuningOptions);
        EnsurePreflightPassed(settings, options);

        log.WriteLine(simulateAudio
            ? "Simulated detection audio mode; this run does not control real system audio."
            : "OCR-driven detection dry-run mode; this run does not control real system audio.");
        log.WriteLine(detectionOcrInputPath is null
            ? $"Live capture from process: {settings.TargetProcessName}"
            : $"Fixed OCR input image: {detectionOcrInputPath}");

        DetectionDryRunOptions dryRunOptions = BuildDryRunOptions(settings, options, tuningOptions);
        WriteDetectionTuning(dryRunOptions, tuningOptions, log);
        WriteOcrBackendState(settings, log);
        IGameWindowCapture windowCapture = await BuildGuiWindowCaptureAsync(
            dryRunOptions,
            hasFixedImageInput: detectionOcrInputPath is not null,
            liveCaptureStartupMode,
            afterForegroundSessionReady,
            log,
            cancellationToken);
        DetectionAudioCoordinator? audioCoordinator = simulateAudio
            ? new DetectionAudioCoordinator(new LoggingAudioMuteService(log, settings.AudioFilter), settings.AudioFilter)
            : null;

        DetectionDryRunLoop loop = new(
            _ocrServices.Get(GetOcrBackendCacheKey(settings)),
            new SpeakerMatcher(),
            windowCapture,
            new OcrInputPreparer(),
            audioCoordinator: audioCoordinator,
            audioActionLabel: "Simulated audio action",
            log: log,
            iterationCompleted: iterationCompleted);

        await loop.RunAsync(dryRunOptions, cancellationToken);
    }

    public async Task RunGuardedRealAudioDetectionAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        await RunGuardedRealAudioDetectionAsync(
            configPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride: null,
            log,
            cancellationToken);
    }

    public async Task RunGuardedRealAudioDetectionAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted = null)
    {
        await RunGuardedRealAudioDetectionCoreAsync(
            configPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride,
            GuiLiveCaptureStartupMode.AutomaticTargetWindow,
            afterForegroundSessionReady: null,
            log,
            cancellationToken,
            iterationCompleted);
    }

    public async Task RunGuardedRealAudioDetectionFromForegroundWindowAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        await RunGuardedRealAudioDetectionFromForegroundWindowAsync(
            configPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride: null,
            afterForegroundSessionReady,
            log,
            cancellationToken);
    }

    public async Task RunGuardedRealAudioDetectionFromForegroundWindowAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted = null)
    {
        await RunGuardedRealAudioDetectionCoreAsync(
            configPath,
            useFixedImageForDetection,
            tuningOptions,
            ocrEngineOverride,
            GuiLiveCaptureStartupMode.ManualForegroundWindow,
            afterForegroundSessionReady,
            log,
            cancellationToken,
            iterationCompleted);
    }

    private async Task RunGuardedRealAudioDetectionCoreAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        OcrEngine? ocrEngineOverride,
        GuiLiveCaptureStartupMode liveCaptureStartupMode,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken,
        Action<DetectionDryRunResult>? iterationCompleted)
    {
        EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection);
        AppCommandLineOptions options = BuildGuardedRealAudioOptions(configPath);
        AppSettings settings = LoadMergedSettings(options);
        ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
        ApplyGuiDetectionTuningOverrides(settings, tuningOptions);
        EnsurePreflightPassed(settings, options);

        log.WriteLine("REAL audio detection mode enabled from GUI after explicit confirmation.");
        log.WriteLine($"Target process: {settings.TargetProcessName}");
        log.WriteLine($"Live capture from process: {settings.TargetProcessName}");
        log.WriteLine($"Audio mode: {settings.AudioFilter.Mode}");
        log.WriteLine($"Volume percent: {settings.AudioFilter.VolumePercent}");
        log.WriteLine("Stable detection, not raw OCR match, drives real audio actions.");

        DetectionDryRunOptions dryRunOptions = BuildDryRunOptions(settings, options, tuningOptions);
        WriteDetectionTuning(dryRunOptions, tuningOptions, log);
        WriteOcrBackendState(settings, log);
        IGameWindowCapture windowCapture = await BuildGuiWindowCaptureAsync(
            dryRunOptions,
            hasFixedImageInput: false,
            liveCaptureStartupMode,
            afterForegroundSessionReady,
            log,
            cancellationToken);
        DetectionAudioCoordinator audioCoordinator = new(
            new WindowsAudioMuteService(settings.TargetProcessName, log, settings.AudioFilter),
            settings.AudioFilter);

        DetectionDryRunLoop loop = new(
            _ocrServices.Get(GetOcrBackendCacheKey(settings)),
            new SpeakerMatcher(),
            windowCapture,
            new OcrInputPreparer(),
            audioCoordinator: audioCoordinator,
            audioActionLabel: "Real audio action",
            log: log,
            iterationCompleted: iterationCompleted);

        await loop.RunAsync(dryRunOptions, cancellationToken);
    }

    private static async Task<IGameWindowCapture> BuildGuiWindowCaptureAsync(
        DetectionDryRunOptions dryRunOptions,
        bool hasFixedImageInput,
        GuiLiveCaptureStartupMode liveCaptureStartupMode,
        Func<Task>? afterForegroundSessionReady,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        WindowsGameWindowCapture capture = new(log);
        if (hasFixedImageInput)
        {
            return capture;
        }

        WindowCaptureOptions captureOptions = BuildWindowCaptureOptions(dryRunOptions);
        IGameWindowCaptureSession session = liveCaptureStartupMode == GuiLiveCaptureStartupMode.ManualForegroundWindow
            ? capture.CreateForegroundSession(captureOptions)
            : capture.CreateSession(captureOptions);
        await session.InitializeAsync(cancellationToken);

        if (liveCaptureStartupMode == GuiLiveCaptureStartupMode.ManualForegroundWindow &&
            afterForegroundSessionReady is not null)
        {
            await afterForegroundSessionReady();
        }

        return new PreinitializedGameWindowCapture(session);
    }

    public GuardedRealAudioStatus GetGuardedRealAudioStatus(string? configPath)
    {
        return GetGuardedRealAudioStatus(configPath, ocrEngineOverride: null);
    }

    public GuardedRealAudioStatus GetGuardedRealAudioStatus(string? configPath, OcrEngine? ocrEngineOverride)
    {
        try
        {
            AppCommandLineOptions options = BuildGuardedRealAudioOptions(configPath);
            AppSettings settings = LoadMergedSettings(options);
            ApplyGuiOcrEngineOverride(settings, ocrEngineOverride);
            RuntimePreflightResult preflightResult = new AppPreflightValidator().Validate(settings, options);
            List<string> issues = preflightResult.Issues
                .Select(issue => $"{issue.Category}: {issue.Message}")
                .ToList();
            return new GuardedRealAudioStatus(
                settings.TargetProcessName,
                settings.AudioFilter.Mode.ToString(),
                settings.AudioFilter.VolumePercent,
                settings.Ocr.GetOcrRegionSourceOptions().HasEffectiveRegionSource,
                preflightResult.Passed && issues.Count == 0,
                issues);
        }
        catch (Exception exception)
        {
            AppSettings? fallbackSettings = TryLoadSettings(configPath);
            ApplyGuiOcrEngineOverride(fallbackSettings, ocrEngineOverride);
            return new GuardedRealAudioStatus(
                TargetProcessName: fallbackSettings?.TargetProcessName ?? string.Empty,
                AudioMode: fallbackSettings?.AudioFilter.Mode.ToString() ?? "Unknown",
                VolumePercent: fallbackSettings?.AudioFilter.VolumePercent ?? 0,
                HasOcrRegionSource: false,
                PreflightPassed: false,
                Issues: [exception.Message]);
        }
    }

    public static string GetDefaultConfigPath()
    {
        if (File.Exists("config.local.json"))
        {
            return "config.local.json";
        }

        return File.Exists("config.example.json")
            ? "config.example.json"
            : string.Empty;
    }

    public static string GetDefaultOcrInputPath()
    {
        return Path.Combine("debug-captures", "capture-latest.png");
    }

    public static string? ResolveDetectionLoopOcrInputPath(string? ocrInputPath, bool useFixedImageForDetection)
    {
        if (!useFixedImageForDetection)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(ocrInputPath))
        {
            throw new ArgumentException("OCR input image path is required when fixed-image detection mode is enabled.");
        }

        return ocrInputPath.Trim();
    }

    public static void EnsureGuardedRealAudioAllowsDetectionInput(bool useFixedImageForDetection)
    {
        if (useFixedImageForDetection)
        {
            throw new InvalidOperationException(
                "Guarded real audio does not allow fixed-image detection. Disable fixed-image detection to use live capture.");
        }
    }

    public static void ApplyGuiDetectionTuningOverrides(AppSettings settings, GuiDetectionTuningOptions tuningOptions)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tuningOptions);

        settings.Detection.LoopCount = tuningOptions.RunUntilStop
            ? null
            : tuningOptions.LoopCount ?? settings.Detection.LoopCount;

        if (tuningOptions.LoopIntervalMs is not null)
        {
            settings.Detection.LoopIntervalMs = tuningOptions.LoopIntervalMs.Value;
        }

        if (tuningOptions.MatchThreshold is not null)
        {
            settings.Detection.MatchThreshold = tuningOptions.MatchThreshold.Value;
        }

        if (tuningOptions.MissThreshold is not null)
        {
            settings.Detection.MissThreshold = tuningOptions.MissThreshold.Value;
        }

        settings.Detection.SaveDebugImages = tuningOptions.SaveDebugImages;
        settings.Detection.SaveOcrFailureSamples = tuningOptions.SaveOcrFailureSamples;
        settings.Detection.EnableInputForegroundFallback = tuningOptions.EnableInputForegroundFallback;
        settings.Validate();
    }

    public static void ApplyGuiOcrEngineOverride(AppSettings? settings, OcrEngine? ocrEngineOverride)
    {
        if (settings is null || ocrEngineOverride is null)
        {
            return;
        }

        settings.Ocr.Engine = ocrEngineOverride.Value;
        settings.Validate();
    }

    private AppSettings LoadSettings(string? configPath)
    {
        return string.IsNullOrWhiteSpace(configPath)
            ? _settingsLoader.LoadDefault()
            : _settingsLoader.LoadFromFile(configPath.Trim());
    }

    private CalibrationCommandContext BuildCalibrationContext(string? configPath)
    {
        AppSettings baseSettings = LoadSettings(configPath);
        AppCommandLineOptions options = BuildOptions(
            configPath,
            "--calibrate-ocr-region",
            "--process",
            baseSettings.TargetProcessName);
        AppSettings settings = LoadMergedSettings(options);

        OcrRegionCalibrationOptions calibrationOptions = new()
        {
            TargetProcessName = settings.TargetProcessName,
            CaptureOutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = options.CaptureDelayMs,
            CalibrationOutputPath = options.CalibrationOutputPath
        };

        return new CalibrationCommandContext(settings, options, calibrationOptions);
    }

    private sealed record CalibrationCommandContext(
        AppSettings Settings,
        AppCommandLineOptions Options,
        OcrRegionCalibrationOptions CalibrationOptions);

    private AppSettings? TryLoadSettings(string? configPath)
    {
        try
        {
            return LoadSettings(configPath);
        }
        catch
        {
            return null;
        }
    }

    private AppSettings LoadMergedSettings(AppCommandLineOptions options)
    {
        AppSettings settings = options.ConfigPath is null
            ? _settingsLoader.LoadDefault()
            : _settingsLoader.LoadFromFile(options.ConfigPath);

        return options.ApplyOverrides(settings);
    }

    private static AppCommandLineOptions BuildOptions(string? configPath, params string[] arguments)
    {
        List<string> parsedArguments = [];
        if (!string.IsNullOrWhiteSpace(configPath))
        {
            parsedArguments.Add("--config");
            parsedArguments.Add(configPath.Trim());
        }

        parsedArguments.AddRange(arguments);
        return AppCommandLineOptions.Parse([.. parsedArguments]);
    }

    private static AppCommandLineOptions BuildGuardedRealAudioOptions(string? configPath)
    {
        List<string> arguments =
        [
            "--detect-loop",
            "--real-audio",
            "--allow-real-audio-from-detection"
        ];

        return BuildOptions(configPath, [.. arguments]);
    }

    private static void EnsurePreflightPassed(AppSettings settings, AppCommandLineOptions options)
    {
        RuntimePreflightResult preflightResult = new AppPreflightValidator().Validate(settings, options);
        if (preflightResult.Passed)
        {
            return;
        }

        string message = string.Join(
            Environment.NewLine,
            preflightResult.Issues.Select(issue => $"{issue.Category}: {issue.Message}"));
        throw new InvalidOperationException(message);
    }

    private async Task<OcrResult> ExtractOcrAsync(
        AppSettings settings,
        AppCommandLineOptions options,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        OcrOptions ocrOptions = new()
        {
            OcrEngine = settings.Ocr.Engine,
            InputImagePath = options.OcrInputPath,
            Language = settings.Ocr.Language,
            TesseractExecutablePath = settings.Ocr.TesseractExecutablePath,
            PaddleModelDirectory = settings.Ocr.PaddleModelDirectory,
            PaddleRuntimeDirectory = settings.Ocr.PaddleRuntimeDirectory,
            PageSegmentationMode = settings.Ocr.PageSegmentationMode,
            InputScale = settings.Ocr.InputScale,
            PaddingPixels = settings.Ocr.PaddingPixels,
            Grayscale = settings.Ocr.Grayscale,
            Invert = settings.Ocr.Invert,
            Threshold = settings.Ocr.Threshold,
            OcrRegion = null
        };

        OcrRegionSourceResolver regionSourceResolver = new();
        ResolvedOcrRegion resolvedRegion = regionSourceResolver.ResolveForImage(
            settings.Ocr.GetOcrRegionSourceOptions(),
            ocrOptions.InputImagePath!);
        ocrOptions.OcrRegion = resolvedRegion.Region;

        log.WriteLine($"OCR input: {ocrOptions.InputImagePath}");
        log.WriteLine($"OCR language: {ocrOptions.Language}, psm: {ocrOptions.PageSegmentationMode}");
        log.WriteLine($"OCR region source: {resolvedRegion.SourceLabel}");
        if (ocrOptions.OcrRegion is not null)
        {
            log.WriteLine($"OCR region: {ocrOptions.OcrRegion}");
        }

        OcrInputPreparer inputPreparer = new();
        string preparedInputPath = inputPreparer.PrepareInput(ocrOptions);
        if (!string.Equals(Path.GetFullPath(ocrOptions.InputImagePath!), preparedInputPath, StringComparison.OrdinalIgnoreCase))
        {
            log.WriteLine($"OCR debug input image: {preparedInputPath}");
        }

        OcrOptions preparedOptions = new()
        {
            OcrEngine = ocrOptions.OcrEngine,
            TesseractExecutablePath = ocrOptions.TesseractExecutablePath,
            PaddleModelDirectory = ocrOptions.PaddleModelDirectory,
            PaddleRuntimeDirectory = ocrOptions.PaddleRuntimeDirectory,
            Language = ocrOptions.Language,
            PageSegmentationMode = ocrOptions.PageSegmentationMode,
            InputImagePath = preparedInputPath
        };

        return await _ocrServices.Get(GetOcrBackendCacheKey(settings)).ExtractTextAsync(preparedOptions, cancellationToken);
    }

    private static OcrOptions BuildOcrOptionsForSettings(AppSettings settings)
    {
        return new OcrOptions
        {
            OcrEngine = settings.Ocr.Engine,
            TesseractExecutablePath = settings.Ocr.TesseractExecutablePath,
            PaddleModelDirectory = settings.Ocr.PaddleModelDirectory,
            PaddleRuntimeDirectory = settings.Ocr.PaddleRuntimeDirectory,
            Language = settings.Ocr.Language,
            PageSegmentationMode = settings.Ocr.PageSegmentationMode,
            InputScale = settings.Ocr.InputScale,
            PaddingPixels = settings.Ocr.PaddingPixels,
            Grayscale = settings.Ocr.Grayscale,
            Invert = settings.Ocr.Invert,
            Threshold = settings.Ocr.Threshold,
            InputImagePath = null
        };
    }

    private static DetectionDryRunOptions BuildDryRunOptions(
        AppSettings settings,
        AppCommandLineOptions options,
        GuiDetectionTuningOptions? tuningOptions = null)
    {
        return new DetectionDryRunOptions
        {
            OcrInputPath = options.OcrInputPath,
            TargetProcessName = settings.TargetProcessName,
            OcrRegion = settings.Ocr.Region,
            OcrRegionConfigPath = settings.Ocr.RegionConfigPath,
            OcrRegionPreset = settings.Ocr.GetOcrRegionSourceOptions().Preset,
            OcrEngine = settings.Ocr.Engine,
            OcrLanguage = settings.Ocr.Language,
            TesseractExecutablePath = settings.Ocr.TesseractExecutablePath,
            PaddleModelDirectory = settings.Ocr.PaddleModelDirectory,
            PaddleRuntimeDirectory = settings.Ocr.PaddleRuntimeDirectory,
            OcrPageSegmentationMode = settings.Ocr.PageSegmentationMode,
            OcrInputScale = settings.Ocr.InputScale,
            OcrPaddingPixels = settings.Ocr.PaddingPixels,
            OcrGrayscale = settings.Ocr.Grayscale,
            OcrInvert = settings.Ocr.Invert,
            OcrThreshold = settings.Ocr.Threshold,
            TargetSpeakers = settings.TargetSpeakers,
            LoopIntervalMs = settings.Detection.LoopIntervalMs,
            LoopCount = settings.Detection.LoopCount,
            CaptureOutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = ResolveGuiCaptureDelayMs(options.CaptureDelayMs, tuningOptions),
            SaveDebugImages = settings.Detection.SaveDebugImages,
            SaveOcrFailureSamples = settings.Detection.SaveOcrFailureSamples,
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = settings.Detection.MatchThreshold,
                MissThreshold = settings.Detection.MissThreshold
            }
        };
    }

    private static WindowCaptureOptions BuildWindowCaptureOptions(DetectionDryRunOptions options)
    {
        return new WindowCaptureOptions
        {
            TargetProcessName = options.TargetProcessName!,
            OutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = options.CaptureDelayMs,
            SaveDebugImage = options.SaveDebugImages
        };
    }

    private static void WriteDetectionTuning(
        DetectionDryRunOptions dryRunOptions,
        GuiDetectionTuningOptions tuningOptions,
        TextWriter log)
    {
        log.WriteLine($"Loop count: {tuningOptions.FormatLoopCount(dryRunOptions.LoopCount)}");
        log.WriteLine($"Loop interval ms: {dryRunOptions.LoopIntervalMs}");
        log.WriteLine($"Capture delay ms: {dryRunOptions.CaptureDelayMs}");
        log.WriteLine($"Match threshold: {dryRunOptions.Stability.MatchThreshold}");
        log.WriteLine($"Miss threshold: {dryRunOptions.Stability.MissThreshold}");
        log.WriteLine($"Save debug images: {dryRunOptions.SaveDebugImages}");
        log.WriteLine($"Save OCR failure samples: {dryRunOptions.SaveOcrFailureSamples}");
        log.WriteLine($"OCR engine: {dryRunOptions.OcrEngine}");
        log.WriteLine(
            $"OCR preprocessing: scale={dryRunOptions.OcrInputScale}, padding={dryRunOptions.OcrPaddingPixels}, grayscale={dryRunOptions.OcrGrayscale}, invert={dryRunOptions.OcrInvert}, threshold={dryRunOptions.OcrThreshold?.ToString() ?? "none"}");
    }

    private void WriteOcrBackendState(AppSettings settings, TextWriter log)
    {
        bool warm = IsOcrBackendWarm(settings);
        log.WriteLine($"OCR backend initialized: {warm}");
        log.WriteLine($"OCR backend warm: {warm}");
        if (settings.Ocr.Engine == OcrEngine.PaddleOcrLocal && !warm)
        {
            log.WriteLine("OCR backend warmup note: PaddleOcrLocal is not warm yet; first OCR may include model initialization. Use Warm up OCR backend before realtime detection.");
        }
    }

    public static int ResolveGuiCaptureDelayMs(int baseCaptureDelayMs, GuiDetectionTuningOptions? tuningOptions)
    {
        return tuningOptions?.CaptureDelayMs ?? baseCaptureDelayMs;
    }

    private static string FormatOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? "(bundled/default)" : path.Trim();
    }

    private bool IsOcrBackendWarm(AppSettings settings)
    {
        return _ocrServices.IsWarm(GetOcrBackendCacheKey(settings));
    }

    private static GuiOcrBackendCacheKey GetOcrBackendCacheKey(AppSettings settings)
    {
        return GuiOcrBackendCacheKey.From(
            settings.Ocr.Engine,
            settings.Ocr.PaddleModelDirectory,
            settings.Ocr.PaddleRuntimeDirectory);
    }

    private static void WritePreflightIssues(RuntimePreflightResult preflightResult, TextWriter log)
    {
        foreach (RuntimePreflightIssue issue in preflightResult.Issues)
        {
            log.WriteLine($"{issue.Category}: {issue.Message}");
        }
    }
}
