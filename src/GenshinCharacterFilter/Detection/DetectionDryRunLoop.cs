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
    private readonly SimulatedDetectionAudioCoordinator? _simulatedAudioCoordinator;
    private readonly TextWriter _log;

    public DetectionDryRunLoop(
        IOcrService ocrService,
        ISpeakerMatcher speakerMatcher,
        IGameWindowCapture windowCapture,
        OcrInputPreparer? ocrInputPreparer = null,
        SimulatedDetectionAudioCoordinator? simulatedAudioCoordinator = null,
        TextWriter? log = null)
    {
        _ocrService = ocrService;
        _speakerMatcher = speakerMatcher;
        _windowCapture = windowCapture;
        _ocrInputPreparer = ocrInputPreparer ?? new OcrInputPreparer();
        _simulatedAudioCoordinator = simulatedAudioCoordinator;
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
        int iteration = 0;

        try
        {
            while (!cancellationToken.IsCancellationRequested &&
                (options.LoopCount is null || iteration < options.LoopCount.Value))
            {
                iteration++;
                DetectionDryRunResult result = await RunIterationAsync(options, stabilityGate, iteration, cancellationToken);
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
            if (_simulatedAudioCoordinator is not null)
            {
                // 退出或取消时仍要尝试恢复模拟状态，避免下一次调试误判当前音频状态。
                SimulatedAudioActionResult shutdownResult =
                    await _simulatedAudioCoordinator.RestoreForShutdownAsync(CancellationToken.None);
                if (shutdownResult.Action != SimulatedAudioAction.None)
                {
                    _log.WriteLine($"Simulated audio shutdown action: {FormatAction(shutdownResult.Action)}");
                }
            }
        }
    }

    private async Task<DetectionDryRunResult> RunIterationAsync(
        DetectionDryRunOptions options,
        DetectionStabilityGate stabilityGate,
        int iteration,
        CancellationToken cancellationToken)
    {
        _log.WriteLine($"Dry-run iteration {iteration} started.");
        string inputImagePath = await ResolveInputImagePathAsync(options, cancellationToken);
        OcrOptions ocrOptions = BuildOcrOptions(options, inputImagePath);
        string preparedInputPath = _ocrInputPreparer.PrepareInput(ocrOptions);

        OcrOptions preparedOptions = BuildOcrOptions(options, preparedInputPath);
        preparedOptions.OcrRegion = null;

        OcrResult ocrResult = await _ocrService.ExtractTextAsync(preparedOptions, cancellationToken);
        SpeakerMatchResult matchResult = _speakerMatcher.Match(
            ocrResult.RawText,
            new SpeakerMatcherOptions
            {
                TargetSpeakers = options.TargetSpeakers
            });
        DetectionStabilityResult stabilityResult = stabilityGate.Observe(matchResult);
        SimulatedAudioActionResult? simulatedAudioActionResult = _simulatedAudioCoordinator is null
            ? null
            : await _simulatedAudioCoordinator.ApplyAsync(stabilityResult, cancellationToken);

        return new DetectionDryRunResult(iteration, ocrResult, matchResult, stabilityResult, simulatedAudioActionResult);
    }

    private async Task<string> ResolveInputImagePathAsync(DetectionDryRunOptions options, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(options.OcrInputPath))
        {
            return options.OcrInputPath;
        }

        WindowCaptureOptions captureOptions = new()
        {
            TargetProcessName = options.TargetProcessName!,
            OutputDirectory = options.CaptureOutputDirectory,
            CaptureDelayMs = options.CaptureDelayMs
        };

        return await _windowCapture.CaptureOnceAsync(captureOptions, cancellationToken);
    }

    private static OcrOptions BuildOcrOptions(DetectionDryRunOptions options, string inputImagePath)
    {
        return new OcrOptions
        {
            InputImagePath = inputImagePath,
            Language = options.OcrLanguage,
            TesseractExecutablePath = options.TesseractExecutablePath,
            PageSegmentationMode = options.OcrPageSegmentationMode,
            OcrRegion = options.OcrRegion
        };
    }

    private void WriteResult(DetectionDryRunResult result)
    {
        _log.WriteLine($"Iteration: {result.Iteration}");
        _log.WriteLine("OCR raw text:");
        _log.WriteLine(result.OcrResult.RawText);
        _log.WriteLine($"Normalized text: {result.SpeakerMatchResult.NormalizedText}");
        _log.WriteLine($"Raw matched: {result.SpeakerMatchResult.Matched}");
        _log.WriteLine($"Raw matched speaker: {result.SpeakerMatchResult.MatchedSpeaker ?? "(none)"}");
        _log.WriteLine($"Stable matched: {result.StabilityResult.StableState.Matched}");
        _log.WriteLine($"Stable matched speaker: {result.StabilityResult.StableState.MatchedSpeaker ?? "(none)"}");
        _log.WriteLine($"Stable state changed: {result.StabilityResult.StableStateChanged}");
        _log.WriteLine($"Consecutive match count: {result.StabilityResult.ConsecutiveMatchCount}");
        _log.WriteLine($"Consecutive miss count: {result.StabilityResult.ConsecutiveMissCount}");
        if (result.SimulatedAudioActionResult is not null)
        {
            _log.WriteLine($"Simulated audio action: {FormatAction(result.SimulatedAudioActionResult.Action)}");
        }

        if (result.StabilityResult.StableStateChanged)
        {
            _log.WriteLine($"Stable state changed: {result.StabilityResult.PreviousStableState} -> {result.StabilityResult.StableState}");
        }

        _log.WriteLine($"Dry-run iteration {result.Iteration} completed.");
    }

    private static string FormatAction(SimulatedAudioAction action)
    {
        return action switch
        {
            SimulatedAudioAction.Mute => "mute",
            SimulatedAudioAction.Restore => "restore",
            _ => "none"
        };
    }
}
