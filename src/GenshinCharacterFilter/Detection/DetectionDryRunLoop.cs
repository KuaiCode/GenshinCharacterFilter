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
    private readonly DetectionAudioCoordinator? _audioCoordinator;
    private readonly string _audioActionLabel;
    private readonly TextWriter _log;

    public DetectionDryRunLoop(
        IOcrService ocrService,
        ISpeakerMatcher speakerMatcher,
        IGameWindowCapture windowCapture,
        OcrInputPreparer? ocrInputPreparer = null,
        DetectionAudioCoordinator? audioCoordinator = null,
        string audioActionLabel = "Simulated audio action",
        TextWriter? log = null)
    {
        _ocrService = ocrService;
        _speakerMatcher = speakerMatcher;
        _windowCapture = windowCapture;
        _ocrInputPreparer = ocrInputPreparer ?? new OcrInputPreparer();
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
            if (_audioCoordinator is not null)
            {
                // 退出或取消时仍要尝试恢复，避免检测驱动的音频状态残留。
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
        DetectionAudioActionResult? audioActionResult = _audioCoordinator is null
            ? null
            : await _audioCoordinator.ApplyAsync(stabilityResult, cancellationToken);

        return new DetectionDryRunResult(iteration, ocrResult, matchResult, stabilityResult, audioActionResult);
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
        if (result.DetectionAudioActionResult is not null)
        {
            _log.WriteLine($"{_audioActionLabel}: {FormatAction(result.DetectionAudioActionResult.Action)}");
        }

        if (result.StabilityResult.StableStateChanged)
        {
            _log.WriteLine($"Stable state changed: {result.StabilityResult.PreviousStableState} -> {result.StabilityResult.StableState}");
        }

        _log.WriteLine($"Dry-run iteration {result.Iteration} completed.");
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

    private static string FormatShutdownActionLabel(string actionLabel)
    {
        const string suffix = " action";
        return actionLabel.EndsWith(suffix, StringComparison.Ordinal)
            ? $"{actionLabel[..^suffix.Length]} shutdown action"
            : $"{actionLabel} shutdown action";
    }
}
