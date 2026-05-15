using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Calibration;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Coordination;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

AppSettings settings;
AppCommandLineOptions commandLineOptions;
bool validateConfigRequested = args.Any(argument => string.Equals(argument, "--validate-config", StringComparison.OrdinalIgnoreCase));

try
{
    commandLineOptions = AppCommandLineOptions.Parse(args);
    AppSettingsLoader settingsLoader = new();
    AppSettings loadedSettings = commandLineOptions.ConfigPath is null
        ? settingsLoader.LoadDefault()
        : settingsLoader.LoadFromFile(commandLineOptions.ConfigPath);

    settings = commandLineOptions.ApplyOverrides(loadedSettings);
}
catch (Exception exception) when (exception is AppSettingsException or ArgumentException)
{
    if (validateConfigRequested)
    {
        Console.Error.WriteLine("Validation failed.");
        Console.Error.WriteLine($"Configuration error: {exception.Message}");
    }
    else
    {
        Console.Error.WriteLine($"Configuration error: {exception.Message}");
    }

    return;
}

if (commandLineOptions.ValidateConfig)
{
    RuntimePreflightResult preflightResult = new AppPreflightValidator().Validate(
        settings,
        commandLineOptions,
        AppPreflightMode.ValidateConfig);

    if (!preflightResult.Passed)
    {
        Console.Error.WriteLine("Validation failed.");
        PrintPreflightIssues(preflightResult);
        return;
    }

    Console.WriteLine("Validation passed.");
    if (commandLineOptions.PrintEffectiveConfig)
    {
        new EffectiveConfigPrinter().Print(settings, commandLineOptions, Console.Out);
    }

    return;
}

if (commandLineOptions.PrintEffectiveConfig)
{
    new EffectiveConfigPrinter().Print(settings, commandLineOptions, Console.Out);
    return;
}

if (commandLineOptions.CalibrateOcrRegion)
{
    if (!RunCommandPreflight(settings, commandLineOptions))
    {
        return;
    }

    await CalibrateOcrRegionAsync(settings, commandLineOptions);
    return;
}

if (commandLineOptions.DetectLoop)
{
    if (!RunCommandPreflight(settings, commandLineOptions))
    {
        return;
    }

    await DetectLoopAsync(settings, commandLineOptions);
    return;
}

if (commandLineOptions.DetectSpeakerOnce)
{
    if (!RunCommandPreflight(settings, commandLineOptions))
    {
        return;
    }

    await DetectSpeakerOnceAsync(settings, commandLineOptions);
    return;
}

if (commandLineOptions.OcrOnce)
{
    if (!RunCommandPreflight(settings, commandLineOptions))
    {
        return;
    }

    await OcrOnceAsync(settings, commandLineOptions);
    return;
}

if (commandLineOptions.CaptureOnce)
{
    if (!RunCommandPreflight(settings, commandLineOptions))
    {
        return;
    }

    await CaptureOnceAsync(settings, commandLineOptions);
    return;
}

using CancellationTokenSource appCancellation = new();
ManualSpeakerDetector speakerDetector = new();
// Default mode uses the simulated audio service; real system audio is controlled only after explicit opt-in.
IAudioMuteService audioMuteService = settings.RealAudioEnabled
    ? new WindowsAudioMuteService(settings.TargetProcessName, Console.Out, settings.AudioFilter)
    : new LoggingAudioMuteService(Console.Out, settings.AudioFilter);
MuteCoordinator coordinator = new(
    speakerDetector,
    audioMuteService,
    new MuteCoordinatorOptions
    {
        TargetSpeakers = new HashSet<string>(settings.TargetSpeakers)
    });

Console.WriteLine("GenshinCharacterFilter v0.13 Usability Hardening");
Console.WriteLine(settings.RealAudioEnabled
    ? $"REAL audio mode enabled for process '{settings.TargetProcessName}'."
    : "Simulation mode; this run does not control real system audio.");
Console.WriteLine($"Audio mode: {settings.AudioFilter.Mode}, volume percent: {settings.AudioFilter.VolumePercent}");
Console.WriteLine($"Target speakers: {string.Join(", ", settings.TargetSpeakers)}");
Console.WriteLine("Enter a speaker name, blank/unknown for no target speaker, or q/quit/exit to leave.");

try
{
    while (true)
    {
        Console.Write("speaker> ");
        string? input = Console.ReadLine();

        if (input is null || IsExitCommand(input))
        {
            break;
        }

        string? speaker = string.Equals(input.Trim(), "unknown", StringComparison.OrdinalIgnoreCase)
            ? null
            : input;

        speakerDetector.SetSpeaker(speaker);
        await coordinator.TickAsync(appCancellation.Token);
        Console.WriteLine($"State: {coordinator.State}");
    }
}
finally
{
    Console.WriteLine("Exiting; attempting restore.");
    await coordinator.RestoreForShutdownAsync(CancellationToken.None);
    Console.WriteLine($"State: {coordinator.State}");
}

static async Task CaptureOnceAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    WindowCaptureOptions captureOptions = new()
    {
        TargetProcessName = settings.TargetProcessName,
        OutputDirectory = commandLineOptions.CaptureOutputDirectory,
        CaptureDelayMs = commandLineOptions.CaptureDelayMs
    };

    IGameWindowCapture capture = new WindowsGameWindowCapture(Console.Out);
    Console.WriteLine("Capture mode; this run does not control real system audio.");

    try
    {
        string outputPath = await capture.CaptureOnceAsync(captureOptions, CancellationToken.None);
        Console.WriteLine($"Capture completed: {outputPath}");
    }
    catch (Exception exception) when (exception is WindowCaptureException or PlatformNotSupportedException)
    {
        Console.Error.WriteLine($"Capture error: {exception.Message}");
    }
}

static async Task OcrOnceAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    Console.WriteLine("OCR mode; this run does not control real system audio.");

    try
    {
        OcrResult result = await ExtractOcrAsync(settings, commandLineOptions);
        PrintOcrResult(result);
    }
    catch (Exception exception) when (exception is OcrException or OcrRegionSourceException or ArgumentException or FileNotFoundException)
    {
        Console.Error.WriteLine($"OCR error: {exception.Message}");
    }
}

static async Task DetectSpeakerOnceAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    Console.WriteLine("Speaker detection mode; this run does not control real system audio.");
    Console.WriteLine("Speaker detection is debug-only and is not connected to MuteCoordinator.");

    try
    {
        string rawText;
        if (commandLineOptions.SpeakerText is not null)
        {
            rawText = commandLineOptions.SpeakerText;
            Console.WriteLine("Speaker text source: --speaker-text");
        }
        else
        {
            Console.WriteLine("Speaker text source: OCR");
            OcrResult ocrResult = await ExtractOcrAsync(settings, commandLineOptions);
            PrintOcrResult(ocrResult);
            rawText = ocrResult.RawText;
        }

        ISpeakerMatcher speakerMatcher = new SpeakerMatcher();
        SpeakerMatchResult matchResult = speakerMatcher.Match(
            rawText,
            new SpeakerMatcherOptions
            {
                TargetSpeakers = settings.TargetSpeakers
            });

        PrintSpeakerMatchResult(matchResult);
    }
    catch (Exception exception) when (exception is OcrException or OcrRegionSourceException or ArgumentException or FileNotFoundException)
    {
        Console.Error.WriteLine($"Speaker detection error: {exception.Message}");
    }
}

static async Task CalibrateOcrRegionAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    if (settings.RealAudioEnabled)
    {
        Console.WriteLine("Real audio setting is ignored in calibration mode.");
    }

    OcrRegionCalibrationOptions calibrationOptions = new()
    {
        TargetProcessName = settings.TargetProcessName,
        CaptureOutputDirectory = commandLineOptions.CaptureOutputDirectory,
        CaptureDelayMs = commandLineOptions.CaptureDelayMs,
        CalibrationOutputPath = commandLineOptions.CalibrationOutputPath
    };

    WindowsOcrRegionCalibrator calibrator = new(
        new WindowsGameWindowCapture(Console.Out),
        Console.Out);

    try
    {
        OcrRegionCalibrationResult result = await calibrator.CalibrateAsync(
            calibrationOptions,
            CancellationToken.None);

        Console.WriteLine($"Calibration output: {calibrationOptions.GetCalibrationOutputPath()}");
        Console.WriteLine($"Source image: {result.SourceImageWidth}x{result.SourceImageHeight}");
        Console.WriteLine($"Region pixels: {result.RegionPixels}");
        Console.WriteLine(
            $"Region ratio: x={result.RegionRatio.X:F6}, y={result.RegionRatio.Y:F6}, width={result.RegionRatio.Width:F6}, height={result.RegionRatio.Height:F6}");
    }
    catch (Exception exception) when (exception is CalibrationException or WindowCaptureException or ArgumentException or IOException or UnauthorizedAccessException or PlatformNotSupportedException)
    {
        Console.Error.WriteLine($"Calibration error: {exception.Message}");
    }
}

static async Task DetectLoopAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    bool realDetectionAudio = commandLineOptions.AllowRealAudioFromDetection;
    Console.WriteLine(realDetectionAudio
        ? "REAL audio detection mode enabled."
        : commandLineOptions.SimulateAudioFromDetection
            ? "Simulated detection audio mode; this run does not control real system audio."
            : "OCR-driven detection dry-run mode; this run does not control real system audio.");
    Console.WriteLine(realDetectionAudio
        ? "Stable detection can request real mute/reduce/restore for the configured target process."
        : commandLineOptions.SimulateAudioFromDetection
            ? "Stable detection can request simulated mute/restore only; WindowsAudioMuteService is not created."
            : "Dry-run output is not connected to MuteCoordinator or automatic mute/restore.");
    if (commandLineOptions.SimulateAudioFromDetection && settings.RealAudioEnabled)
    {
        Console.WriteLine("Real audio setting is ignored in simulated detection audio mode.");
    }
    if (realDetectionAudio)
    {
        Console.WriteLine("WARNING: real Windows audio will be controlled from stable detection results.");
        Console.WriteLine($"Target process: {settings.TargetProcessName}");
        Console.WriteLine($"Audio mode: {settings.AudioFilter.Mode}, volume percent: {settings.AudioFilter.VolumePercent}");
    }

    using CancellationTokenSource cancellation = new();
    ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        cancellation.Cancel();
        Console.WriteLine("Cancellation requested; stopping dry-run loop.");
    };
    Console.CancelKeyPress += cancelHandler;

    try
    {
        DetectionDryRunOptions dryRunOptions = new()
        {
            OcrInputPath = commandLineOptions.OcrInputPath,
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
            CaptureOutputDirectory = commandLineOptions.CaptureOutputDirectory,
            CaptureDelayMs = commandLineOptions.CaptureDelayMs,
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = settings.Detection.MatchThreshold,
                MissThreshold = settings.Detection.MissThreshold
            }
        };

        Console.WriteLine($"Loop interval: {dryRunOptions.LoopIntervalMs} ms");
        Console.WriteLine($"Loop count: {(dryRunOptions.LoopCount?.ToString() ?? "until Ctrl+C")}");
        Console.WriteLine($"Match threshold: {dryRunOptions.Stability.MatchThreshold}");
        Console.WriteLine($"Miss threshold: {dryRunOptions.Stability.MissThreshold}");

        DetectionAudioCoordinator? audioCoordinator = null;
        string audioActionLabel = "Simulated audio action";
        if (realDetectionAudio)
        {
            audioCoordinator = new DetectionAudioCoordinator(
                new WindowsAudioMuteService(settings.TargetProcessName, Console.Out, settings.AudioFilter),
                settings.AudioFilter);
            audioActionLabel = "Real audio action";
        }
        else if (commandLineOptions.SimulateAudioFromDetection)
        {
            audioCoordinator = new DetectionAudioCoordinator(
                new LoggingAudioMuteService(Console.Out, settings.AudioFilter),
                settings.AudioFilter);
        }

        DetectionDryRunLoop loop = new(
            new TesseractCliOcrService(),
            new SpeakerMatcher(),
            new WindowsGameWindowCapture(Console.Out),
            new OcrInputPreparer(),
            audioCoordinator: audioCoordinator,
            audioActionLabel: audioActionLabel,
            log: Console.Out);

        await loop.RunAsync(dryRunOptions, cancellation.Token);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        Console.WriteLine("Detect loop stopped.");
    }
    catch (Exception exception) when (exception is OcrException or OcrRegionSourceException or WindowCaptureException or ArgumentException or FileNotFoundException or PlatformNotSupportedException)
    {
        Console.Error.WriteLine($"Detect loop error: {exception.Message}");
    }
    finally
    {
        Console.CancelKeyPress -= cancelHandler;
    }
}

static async Task<OcrResult> ExtractOcrAsync(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    OcrOptions ocrOptions = new()
    {
        OcrEngine = settings.Ocr.Engine,
        InputImagePath = commandLineOptions.OcrInputPath,
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

    OcrInputPreparer inputPreparer = new();
    IOcrService ocrService = new TesseractCliOcrService();
    Console.WriteLine($"OCR input: {ocrOptions.InputImagePath}");
    Console.WriteLine($"OCR language: {ocrOptions.Language}, psm: {ocrOptions.PageSegmentationMode}");
    Console.WriteLine($"OCR region source: {resolvedRegion.SourceLabel}");
    if (ocrOptions.OcrRegion is not null)
    {
        Console.WriteLine($"OCR region: {ocrOptions.OcrRegion}");
    }

    string preparedInputPath = inputPreparer.PrepareInput(ocrOptions);
    if (!string.Equals(Path.GetFullPath(ocrOptions.InputImagePath!), preparedInputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"OCR debug input image: {preparedInputPath}");
    }

    OcrOptions preparedOptions = new()
    {
        OcrEngine = ocrOptions.OcrEngine,
        TesseractExecutablePath = ocrOptions.TesseractExecutablePath,
        Language = ocrOptions.Language,
        PageSegmentationMode = ocrOptions.PageSegmentationMode,
        InputImagePath = preparedInputPath
    };

    return await ocrService.ExtractTextAsync(preparedOptions, CancellationToken.None);
}

static void PrintOcrResult(OcrResult result)
{
    Console.WriteLine($"OCR engine: {result.EngineName}");
    Console.WriteLine($"OCR input path: {result.InputImagePath}");
    Console.WriteLine("OCR raw text:");
    Console.WriteLine(result.RawText);
}

static void PrintSpeakerMatchResult(SpeakerMatchResult result)
{
    Console.WriteLine("Raw text:");
    Console.WriteLine(result.RawText);
    Console.WriteLine($"Normalized text: {result.NormalizedText}");
    Console.WriteLine($"Matched: {result.Matched}");
    Console.WriteLine($"Matched speaker: {result.MatchedSpeaker ?? "(none)"}");
}

static bool IsExitCommand(string input)
{
    string command = input.Trim();

    return string.Equals(command, "q", StringComparison.OrdinalIgnoreCase)
        || string.Equals(command, "quit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(command, "exit", StringComparison.OrdinalIgnoreCase);
}

static bool RunCommandPreflight(AppSettings settings, AppCommandLineOptions commandLineOptions)
{
    RuntimePreflightResult preflightResult = new AppPreflightValidator().Validate(settings, commandLineOptions);
    if (preflightResult.Passed)
    {
        return true;
    }

    PrintPreflightIssues(preflightResult);
    return false;
}

static void PrintPreflightIssues(RuntimePreflightResult preflightResult)
{
    foreach (RuntimePreflightIssue issue in preflightResult.Issues)
    {
        Console.Error.WriteLine($"{issue.Category}: {issue.Message}");
    }
}
