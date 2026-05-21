using System.Diagnostics;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Runs OCR + speaker matching repeatedly for debug observation without audio control.
/// </summary>
public sealed class DetectionDryRunLoop
{
    private readonly IOcrService _ocrService;
    private readonly ISpeakerMatcher _speakerMatcher;
    private readonly IGameWindowCapture _windowCapture;
    private readonly OcrInputPreparer _ocrInputPreparer;
    private readonly OcrRegionSourceResolver _ocrRegionSourceResolver;
    private readonly OcrFailureSampleSaver _failureSampleSaver;
    private readonly DetectionAudioCoordinator? _audioCoordinator;
    private readonly string _audioActionLabel;
    private readonly TextWriter _log;

    public DetectionDryRunLoop(
        IOcrService ocrService,
        ISpeakerMatcher speakerMatcher,
        IGameWindowCapture windowCapture,
        OcrInputPreparer? ocrInputPreparer = null,
        OcrRegionSourceResolver? ocrRegionSourceResolver = null,
        OcrFailureSampleSaver? failureSampleSaver = null,
        DetectionAudioCoordinator? audioCoordinator = null,
        string audioActionLabel = "Simulated audio action",
        TextWriter? log = null)
    {
        _ocrService = ocrService;
        _speakerMatcher = speakerMatcher;
        _windowCapture = windowCapture;
        _ocrInputPreparer = ocrInputPreparer ?? new OcrInputPreparer();
        _ocrRegionSourceResolver = ocrRegionSourceResolver ?? new OcrRegionSourceResolver();
        _failureSampleSaver = failureSampleSaver ?? new OcrFailureSampleSaver();
        _audioCoordinator = audioCoordinator;
        _audioActionLabel = string.IsNullOrWhiteSpace(audioActionLabel)
            ? "Audio action"
            : audioActionLabel;
        _log = log ?? TextWriter.Null;
    }

    /// <summary>
    /// Runs the dry-run loop until the configured count is reached or cancellation is requested.
    /// </summary>
    public async Task RunAsync(DetectionDryRunOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        DetectionStabilityGate stabilityGate = new(options.Stability);
        _log.WriteLine($"OCR engine: {options.OcrEngine}");
        _log.WriteLine($"OCR backend initialized: {IsOcrBackendInitialized()}");
        _log.WriteLine($"OCR backend warm: {IsOcrBackendWarm()}");
        if (options.OcrEngine == OcrEngine.PaddleOcrLocal)
        {
            _log.WriteLine($"Paddle model path: {FormatOptionalPath(options.PaddleModelDirectory)}");
            _log.WriteLine($"Paddle runtime path: {FormatOptionalPath(options.PaddleRuntimeDirectory)}");
        }
        _log.WriteLine(
            $"OCR preprocessing: scale={options.OcrInputScale}, padding={options.OcrPaddingPixels}, grayscale={options.OcrGrayscale}, invert={options.OcrInvert}, threshold={options.OcrThreshold?.ToString() ?? "none"}");
        _log.WriteLine($"Save OCR failure samples: {options.SaveOcrFailureSamples}");
        using IGameWindowCaptureSession? liveCaptureSession = CreateLiveCaptureSession(options);
        if (liveCaptureSession is not null)
        {
            await liveCaptureSession.InitializeAsync(cancellationToken);
        }

        int iteration = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                (options.LoopCount is null || iteration < options.LoopCount.Value))
            {
                iteration++;
                DetectionDryRunResult result = await RunIterationAsync(
                    options,
                    stabilityGate,
                    liveCaptureSession,
                    iteration,
                    cancellationToken);
                WriteResult(result);

                if (options.LoopCount is not null && iteration >= options.LoopCount.Value)
                {
                    break;
                }

                await Task.Delay(options.LoopIntervalMs, cancellationToken);
            }
        }
        finally
        {
            if (_audioCoordinator is not null)
            {
                // Cancellation or shutdown must still try restore to avoid leaving detection-driven audio filtered.
                DetectionAudioActionResult shutdownResult =
                    await _audioCoordinator.RestoreForShutdownAsync(CancellationToken.None);
                if (shutdownResult.Action != DetectionAudioAction.None)
                {
                    _log.WriteLine($"{FormatShutdownActionLabel(_audioActionLabel)}: {FormatAction(shutdownResult.Action)}");
                }
            }
        }
    }

    private async Task<DetectionDryRunResult> RunIterationAsync(
        DetectionDryRunOptions options,
        DetectionStabilityGate stabilityGate,
        IGameWindowCaptureSession? liveCaptureSession,
        int iteration,
        CancellationToken cancellationToken)
    {
        _log.WriteLine($"Dry-run iteration {iteration} started.");
        Stopwatch totalStopwatch = Stopwatch.StartNew();
        Stopwatch captureStopwatch = Stopwatch.StartNew();
        IterationImageInput input = await ResolveIterationImageInputAsync(options, liveCaptureSession, cancellationToken);
        captureStopwatch.Stop();
        _log.WriteLine($"Capture mode: {input.CaptureMode}");
        string? preparedInputPath = null;

        try
        {
            Stopwatch ocrStopwatch = Stopwatch.StartNew();
            _log.WriteLine($"OCR region source: {input.OcrRegionSourceLabel}");

            OcrOptions ocrOptions = BuildOcrOptions(options, input.ImagePath, input.OcrRegion);
            preparedInputPath = _ocrInputPreparer.PrepareInput(ocrOptions);

            OcrOptions preparedOptions = BuildOcrOptions(options, preparedInputPath, null);
            preparedOptions.OcrRegion = null;

            OcrResult ocrResult = await _ocrService.ExtractTextAsync(preparedOptions, cancellationToken);
            ocrStopwatch.Stop();

            Stopwatch matchStopwatch = Stopwatch.StartNew();
            SpeakerMatchResult matchResult = _speakerMatcher.Match(
                ocrResult.RawText,
                new SpeakerMatcherOptions
                {
                    TargetSpeakers = options.TargetSpeakers
                });
            DetectionStabilityResult stabilityResult = stabilityGate.Observe(matchResult);
            matchStopwatch.Stop();

            TrySaveOcrFailureSample(
                options,
                iteration,
                preparedInputPath,
                input.MetadataRegion,
                ocrResult,
                matchResult,
                ocrStopwatch.ElapsedMilliseconds);

            Stopwatch audioStopwatch = Stopwatch.StartNew();
            DetectionAudioActionResult? audioActionResult = _audioCoordinator is null
                ? null
                : await _audioCoordinator.ApplyAsync(stabilityResult, cancellationToken);
            audioStopwatch.Stop();
            totalStopwatch.Stop();

            DetectionIterationTiming timing = new(
                captureStopwatch.ElapsedMilliseconds,
                ocrStopwatch.ElapsedMilliseconds,
                matchStopwatch.ElapsedMilliseconds,
                audioStopwatch.ElapsedMilliseconds,
                totalStopwatch.ElapsedMilliseconds);

            return new DetectionDryRunResult(
                iteration,
                ocrResult,
                matchResult,
                stabilityResult,
                input.OcrRegionSourceLabel,
                audioActionResult,
                timing);
        }
        finally
        {
            DeleteRealtimeTempFiles(options, input.ImagePath, preparedInputPath, input.DeleteCaptureInputAfterUse);
        }
    }

    private IGameWindowCaptureSession? CreateLiveCaptureSession(DetectionDryRunOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OcrInputPath) ||
            _windowCapture is not IGameWindowCaptureSessionFactory sessionFactory)
        {
            return null;
        }

        return sessionFactory.CreateSession(BuildWindowCaptureOptions(options));
    }

    private async Task<IterationImageInput> ResolveIterationImageInputAsync(
        DetectionDryRunOptions options,
        IGameWindowCaptureSession? liveCaptureSession,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.OcrInputPath))
        {
            ResolvedOcrRegion resolvedRegion = _ocrRegionSourceResolver.ResolveForImage(
                options.GetOcrRegionSourceOptions(),
                options.OcrInputPath);
            return new IterationImageInput(
                options.OcrInputPath,
                resolvedRegion.Region,
                resolvedRegion.Region,
                resolvedRegion.SourceLabel,
                "fixed image",
                DeleteCaptureInputAfterUse: false);
        }

        if (liveCaptureSession is not null)
        {
            IterationImageInput? regionOnlyInput = await TryResolveRegionOnlyInputAsync(
                options,
                liveCaptureSession,
                cancellationToken);
            if (regionOnlyInput is not null)
            {
                return regionOnlyInput;
            }

            string fullWindowPath = await liveCaptureSession.CaptureAsync(cancellationToken);
            return ResolveFullWindowInput(options, fullWindowPath, "full-window fallback");
        }

        string oneShotPath = await _windowCapture.CaptureOnceAsync(BuildWindowCaptureOptions(options), cancellationToken);
        return ResolveFullWindowInput(options, oneShotPath, "full-window fallback");
    }

    private async Task<IterationImageInput?> TryResolveRegionOnlyInputAsync(
        DetectionDryRunOptions options,
        IGameWindowCaptureSession liveCaptureSession,
        CancellationToken cancellationToken)
    {
        if (!options.GetOcrRegionSourceOptions().HasEffectiveRegionSource)
        {
            return null;
        }

        try
        {
            WindowCaptureFrameInfo frameInfo = await liveCaptureSession.GetFrameInfoAsync(cancellationToken);
            ResolvedOcrRegion resolvedRegion = _ocrRegionSourceResolver.Resolve(
                options.GetOcrRegionSourceOptions(),
                frameInfo.Width,
                frameInfo.Height);
            if (resolvedRegion.Region is null)
            {
                _log.WriteLine("Region-only capture fallback reason: OCR region source resolved to full image.");
                return null;
            }

            string regionImagePath = await liveCaptureSession.CaptureRegionAsync(
                ToCaptureRegion(resolvedRegion.Region.Value),
                cancellationToken);
            string captureModePrefix = liveCaptureSession is IGameWindowCaptureSessionMetadata metadata
                ? metadata.CaptureModePrefix
                : "process";
            return new IterationImageInput(
                ImagePath: regionImagePath,
                OcrRegion: null,
                MetadataRegion: resolvedRegion.Region,
                OcrRegionSourceLabel: resolvedRegion.SourceLabel,
                CaptureMode: $"{captureModePrefix}-region-only",
                DeleteCaptureInputAfterUse: ShouldDeleteRealtimeCaptureInput(options, regionImagePath));
        }
        catch (Exception exception) when (exception is OcrRegionSourceException or WindowCaptureException or ArgumentException)
        {
            _log.WriteLine($"Region-only capture fallback reason: {exception.Message}");
            return null;
        }
    }

    private IterationImageInput ResolveFullWindowInput(
        DetectionDryRunOptions options,
        string inputImagePath,
        string captureMode)
    {
        ResolvedOcrRegion resolvedRegion = _ocrRegionSourceResolver.ResolveForImage(
            options.GetOcrRegionSourceOptions(),
            inputImagePath);
        return new IterationImageInput(
            inputImagePath,
            resolvedRegion.Region,
            resolvedRegion.Region,
            resolvedRegion.SourceLabel,
            captureMode,
            ShouldDeleteRealtimeCaptureInput(options, inputImagePath));
    }

    private static CaptureRegion ToCaptureRegion(OcrRegion region)
    {
        return new CaptureRegion(region.X, region.Y, region.Width, region.Height);
    }

    private static WindowCaptureOptions BuildWindowCaptureOptions(DetectionDryRunOptions options)
    {
        WindowCaptureOptions captureOptions = new()
        {
            TargetProcessName = options.TargetProcessName!,
            OutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = options.CaptureDelayMs,
            SaveDebugImage = options.SaveDebugImages
        };

        return captureOptions;
    }

    private static OcrOptions BuildOcrOptions(
        DetectionDryRunOptions options,
        string inputImagePath,
        OcrRegion? ocrRegion)
    {
        return new OcrOptions
        {
            OcrEngine = options.OcrEngine,
            InputImagePath = inputImagePath,
            Language = options.OcrLanguage,
            TesseractExecutablePath = options.TesseractExecutablePath,
            PaddleModelDirectory = options.PaddleModelDirectory,
            PaddleRuntimeDirectory = options.PaddleRuntimeDirectory,
            PageSegmentationMode = options.OcrPageSegmentationMode,
            OcrRegion = ocrRegion,
            SaveDebugImage = options.SaveDebugImages,
            InputScale = options.OcrInputScale,
            PaddingPixels = options.OcrPaddingPixels,
            Grayscale = options.OcrGrayscale,
            Invert = options.OcrInvert,
            Threshold = options.OcrThreshold
        };
    }

    private void TrySaveOcrFailureSample(
        DetectionDryRunOptions options,
        int iteration,
        string? preparedInputPath,
        OcrRegion? region,
        OcrResult ocrResult,
        SpeakerMatchResult matchResult,
        long ocrElapsedMs)
    {
        if (!options.SaveOcrFailureSamples ||
            string.IsNullOrWhiteSpace(preparedInputPath) ||
            !OcrFailureSampleSaver.ShouldSave(matchResult))
        {
            return;
        }

        try
        {
            OcrFailureSampleResult result = _failureSampleSaver.Save(
                preparedInputPath,
                new OcrFailureSampleMetadata(
                    DateTimeOffset.Now,
                    options.OcrEngine.ToString(),
                    ocrResult.RawText,
                    matchResult.NormalizedText,
                    options.TargetSpeakers.ToArray(),
                    region,
                    ocrElapsedMs,
                    iteration));
            _log.WriteLine($"OCR failure sample saved: {result.ImagePath}");
            _log.WriteLine($"OCR failure metadata saved: {result.MetadataPath}");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OcrException)
        {
            _log.WriteLine($"OCR failure sample warning: {exception.Message}");
        }
    }

    private static bool ShouldDeleteRealtimeCaptureInput(DetectionDryRunOptions options, string inputImagePath)
    {
        return !options.SaveDebugImages &&
            string.IsNullOrWhiteSpace(options.OcrInputPath) &&
            IsUnderDirectory(inputImagePath, WindowCaptureOptions.GetTempCaptureDirectory());
    }

    private static void DeleteRealtimeTempFiles(
        DetectionDryRunOptions options,
        string inputImagePath,
        string? preparedInputPath,
        bool deleteCaptureInputAfterUse)
    {
        if (options.SaveDebugImages)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(preparedInputPath) &&
            !string.Equals(Path.GetFullPath(preparedInputPath), Path.GetFullPath(inputImagePath), StringComparison.OrdinalIgnoreCase) &&
            IsUnderDirectory(preparedInputPath, OcrInputPreparer.GetTempOcrInputDirectory()))
        {
            TryDeleteTempFile(preparedInputPath);
        }

        if (deleteCaptureInputAfterUse)
        {
            TryDeleteTempFile(inputImagePath);
        }
    }

    private static bool IsUnderDirectory(string path, string directory)
    {
        string fullPath = Path.GetFullPath(path);
        string fullDirectory = Path.GetFullPath(directory);
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar))
        {
            fullDirectory += Path.DirectorySeparatorChar;
        }

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDeleteTempFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private void WriteResult(DetectionDryRunResult result)
    {
        _log.WriteLine($"Iteration: {result.Iteration}");
        _log.WriteLine("OCR raw text:");
        _log.WriteLine(result.OcrResult.RawText);
        _log.WriteLine($"OCR region source: {result.OcrRegionSourceLabel}");
        _log.WriteLine($"Normalized text: {result.SpeakerMatchResult.NormalizedText}");
        _log.WriteLine($"Raw matched: {result.SpeakerMatchResult.Matched}");
        _log.WriteLine($"Raw match kind: {result.SpeakerMatchResult.MatchKind}");
        _log.WriteLine($"Raw matched speaker: {result.SpeakerMatchResult.MatchedSpeaker ?? "(none)"}");
        _log.WriteLine($"Stable matched: {result.StabilityResult.StableState.Matched}");
        _log.WriteLine($"Stable matched speaker: {result.StabilityResult.StableState.MatchedSpeaker ?? "(none)"}");
        _log.WriteLine($"Stable state changed: {result.StabilityResult.StableStateChanged}");
        _log.WriteLine($"Consecutive match count: {result.StabilityResult.ConsecutiveMatchCount}");
        _log.WriteLine($"Consecutive miss count: {result.StabilityResult.ConsecutiveMissCount}");
        if (result.DetectionAudioActionResult is not null)
        {
            _log.WriteLine($"{_audioActionLabel}: {FormatAction(result.DetectionAudioActionResult.Action)}");
        }

        WriteTiming(result.Timing);

        if (result.StabilityResult.StableStateChanged)
        {
            _log.WriteLine($"Stable state changed: {result.StabilityResult.PreviousStableState} -> {result.StabilityResult.StableState}");
        }

        _log.WriteLine($"Dry-run iteration {result.Iteration} completed.");
    }

    private void WriteTiming(DetectionIterationTiming? timing)
    {
        if (timing is null)
        {
            return;
        }

        _log.WriteLine("Iteration timing:");
        _log.WriteLine($"  Capture: {timing.CaptureElapsedMs} ms");
        _log.WriteLine($"  OCR: {timing.OcrElapsedMs} ms");
        _log.WriteLine($"  Match: {timing.MatchElapsedMs} ms");
        _log.WriteLine($"  Audio: {timing.AudioElapsedMs} ms");
        _log.WriteLine($"  Total: {timing.TotalElapsedMs} ms");
    }

    private static string FormatAction(DetectionAudioAction action)
    {
        return action switch
        {
            DetectionAudioAction.Mute => "mute",
            DetectionAudioAction.Reduce => "reduce",
            DetectionAudioAction.Restore => "restore",
            _ => "none"
        };
    }

    private bool IsOcrBackendInitialized()
    {
        return _ocrService is PaddleOcrLocalService paddle
            ? paddle.IsInitialized
            : true;
    }

    private bool IsOcrBackendWarm()
    {
        return _ocrService is IOcrBackendWarmup warmup
            ? warmup.IsWarm
            : true;
    }

    private static string FormatOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? "(bundled/default)" : path.Trim();
    }

    private static string FormatShutdownActionLabel(string actionLabel)
    {
        const string suffix = " action";
        return actionLabel.EndsWith(suffix, StringComparison.Ordinal)
            ? $"{actionLabel[..^suffix.Length]} shutdown action"
            : $"{actionLabel} shutdown action";
    }

    private sealed record IterationImageInput(
        string ImagePath,
        OcrRegion? OcrRegion,
        OcrRegion? MetadataRegion,
        string OcrRegionSourceLabel,
        string CaptureMode,
        bool DeleteCaptureInputAfterUse);
}
