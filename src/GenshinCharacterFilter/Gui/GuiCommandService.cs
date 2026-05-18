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
        AppCommandLineOptions options = BuildOptions(configPath, "--print-effective-config");
        AppSettings settings = LoadMergedSettings(options);
        new EffectiveConfigPrinter().Print(settings, options, log);
    }

    public async Task CalibrateOcrRegionAsync(string? configPath, TextWriter log, CancellationToken cancellationToken)
    {
        AppSettings settings = LoadSettings(configPath);
        AppCommandLineOptions options = BuildOptions(
            configPath,
            "--calibrate-ocr-region",
            "--process",
            settings.TargetProcessName);
        settings = LoadMergedSettings(options);

        EnsurePreflightPassed(settings, options);
        log.WriteLine("Starting OCR region calibration.");

        OcrRegionCalibrationOptions calibrationOptions = new()
        {
            TargetProcessName = settings.TargetProcessName,
            CaptureOutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = options.CaptureDelayMs,
            CalibrationOutputPath = options.CalibrationOutputPath
        };

        WindowsOcrRegionCalibrator calibrator = new(
            new WindowsGameWindowCapture(log),
            log);

        OcrRegionCalibrationResult result = await calibrator.CalibrateAsync(calibrationOptions, cancellationToken);
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
        AppCommandLineOptions options = BuildOptions(configPath, "--ocr-once", "--ocr-input", ocrInputPath);
        AppSettings settings = LoadMergedSettings(options);
        EnsurePreflightPassed(settings, options);

        log.WriteLine("OCR mode; this run does not control real system audio.");
        OcrResult result = await ExtractOcrAsync(settings, options, log, cancellationToken);
        log.WriteLine($"OCR engine: {result.EngineName}");
        log.WriteLine($"OCR input path: {result.InputImagePath}");
        log.WriteLine("OCR raw text:");
        log.WriteLine(result.RawText);
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
        DetectionAudioCoordinator? audioCoordinator = simulateAudio
            ? new DetectionAudioCoordinator(new LoggingAudioMuteService(log, settings.AudioFilter), settings.AudioFilter)
            : null;

        DetectionDryRunLoop loop = new(
            new TesseractCliOcrService(),
            new SpeakerMatcher(),
            new WindowsGameWindowCapture(log),
            new OcrInputPreparer(),
            audioCoordinator: audioCoordinator,
            audioActionLabel: "Simulated audio action",
            log: log);

        await loop.RunAsync(dryRunOptions, cancellationToken);
    }

    public async Task RunGuardedRealAudioDetectionAsync(
        string? configPath,
        bool useFixedImageForDetection,
        GuiDetectionTuningOptions tuningOptions,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection);
        AppCommandLineOptions options = BuildGuardedRealAudioOptions(configPath);
        AppSettings settings = LoadMergedSettings(options);
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
        DetectionAudioCoordinator audioCoordinator = new(
            new WindowsAudioMuteService(settings.TargetProcessName, log, settings.AudioFilter),
            settings.AudioFilter);

        DetectionDryRunLoop loop = new(
            new TesseractCliOcrService(),
            new SpeakerMatcher(),
            new WindowsGameWindowCapture(log),
            new OcrInputPreparer(),
            audioCoordinator: audioCoordinator,
            audioActionLabel: "Real audio action",
            log: log);

        await loop.RunAsync(dryRunOptions, cancellationToken);
    }

    public GuardedRealAudioStatus GetGuardedRealAudioStatus(string? configPath)
    {
        try
        {
            AppCommandLineOptions options = BuildGuardedRealAudioOptions(configPath);
            AppSettings settings = LoadMergedSettings(options);
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
        settings.Validate();
    }

    private AppSettings LoadSettings(string? configPath)
    {
        return string.IsNullOrWhiteSpace(configPath)
            ? _settingsLoader.LoadDefault()
            : _settingsLoader.LoadFromFile(configPath.Trim());
    }

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

    private static async Task<OcrResult> ExtractOcrAsync(
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
            PageSegmentationMode = settings.Ocr.PageSegmentationMode,
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
            Language = ocrOptions.Language,
            PageSegmentationMode = ocrOptions.PageSegmentationMode,
            InputImagePath = preparedInputPath
        };

        return await new TesseractCliOcrService().ExtractTextAsync(preparedOptions, cancellationToken);
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
            OcrPageSegmentationMode = settings.Ocr.PageSegmentationMode,
            TargetSpeakers = settings.TargetSpeakers,
            LoopIntervalMs = settings.Detection.LoopIntervalMs,
            LoopCount = settings.Detection.LoopCount,
            CaptureOutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = ResolveGuiCaptureDelayMs(options.CaptureDelayMs, tuningOptions),
            SaveDebugImages = settings.Detection.SaveDebugImages,
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = settings.Detection.MatchThreshold,
                MissThreshold = settings.Detection.MissThreshold
            }
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
    }

    public static int ResolveGuiCaptureDelayMs(int baseCaptureDelayMs, GuiDetectionTuningOptions? tuningOptions)
    {
        return tuningOptions?.CaptureDelayMs ?? baseCaptureDelayMs;
    }

    private static void WritePreflightIssues(RuntimePreflightResult preflightResult, TextWriter log)
    {
        foreach (RuntimePreflightIssue issue in preflightResult.Issues)
        {
            log.WriteLine($"{issue.Category}: {issue.Message}");
        }
    }
}
