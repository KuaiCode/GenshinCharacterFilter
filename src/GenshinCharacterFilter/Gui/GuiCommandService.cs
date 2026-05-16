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
        bool simulateAudio,
        TextWriter log,
        CancellationToken cancellationToken)
    {
        List<string> arguments = ["--detect-loop"];
        if (simulateAudio)
        {
            arguments.Add("--simulate-audio-from-detection");
        }

        if (!string.IsNullOrWhiteSpace(ocrInputPath))
        {
            arguments.Add("--ocr-input");
            arguments.Add(ocrInputPath.Trim());
        }

        AppCommandLineOptions options = BuildOptions(configPath, [.. arguments]);
        AppSettings settings = LoadMergedSettings(options);
        EnsurePreflightPassed(settings, options);

        log.WriteLine(simulateAudio
            ? "Simulated detection audio mode; this run does not control real system audio."
            : "OCR-driven detection dry-run mode; this run does not control real system audio.");

        DetectionDryRunOptions dryRunOptions = BuildDryRunOptions(settings, options);
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

    private AppSettings LoadSettings(string? configPath)
    {
        return string.IsNullOrWhiteSpace(configPath)
            ? _settingsLoader.LoadDefault()
            : _settingsLoader.LoadFromFile(configPath.Trim());
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

    private static DetectionDryRunOptions BuildDryRunOptions(AppSettings settings, AppCommandLineOptions options)
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
            CaptureDelayMs = options.CaptureDelayMs,
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = settings.Detection.MatchThreshold,
                MissThreshold = settings.Detection.MissThreshold
            }
        };
    }

    private static void WritePreflightIssues(RuntimePreflightResult preflightResult, TextWriter log)
    {
        foreach (RuntimePreflightIssue issue in preflightResult.Issues)
        {
            log.WriteLine($"{issue.Category}: {issue.Message}");
        }
    }
}
