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
    private readonly TextWriter _log;

    public DetectionDryRunLoop(
        IOcrService ocrService,
        ISpeakerMatcher speakerMatcher,
        IGameWindowCapture windowCapture,
        OcrInputPreparer? ocrInputPreparer = null,
        TextWriter? log = null)
    {
        _ocrService = ocrService;
        _speakerMatcher = speakerMatcher;
        _windowCapture = windowCapture;
        _ocrInputPreparer = ocrInputPreparer ?? new OcrInputPreparer();
        _log = log ?? TextWriter.Null;
    }

    /// <summary>
    /// Runs the dry-run loop until the configured count is reached or cancellation is requested.
    /// </summary>
    public async Task RunAsync(DetectionDryRunOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        DetectionDryRunState? previousState = null;
        int iteration = 0;

        while (!cancellationToken.IsCancellationRequested &&
            (options.LoopCount is null || iteration < options.LoopCount.Value))
        {
            iteration++;
            DetectionDryRunResult result = await RunIterationAsync(options, iteration, previousState, cancellationToken);
            WriteResult(result);
            previousState = result.CurrentState;

            if (options.LoopCount is not null && iteration >= options.LoopCount.Value)
            {
                break;
            }

            await Task.Delay(options.LoopIntervalMs, cancellationToken);
        }
    }

    private async Task<DetectionDryRunResult> RunIterationAsync(
        DetectionDryRunOptions options,
        int iteration,
        DetectionDryRunState? previousState,
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
        DetectionDryRunState currentState = DetectionDryRunState.FromMatch(matchResult);
        bool stateChanged = currentState.IsStateChangeFrom(previousState);

        return new DetectionDryRunResult(iteration, ocrResult, matchResult, currentState, previousState, stateChanged);
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
        _log.WriteLine($"Matched: {result.SpeakerMatchResult.Matched}");
        _log.WriteLine($"Matched speaker: {result.SpeakerMatchResult.MatchedSpeaker ?? "(none)"}");
        _log.WriteLine($"State changed since previous iteration: {result.StateChanged}");

        if (result.StateChanged && result.PreviousState is not null)
        {
            _log.WriteLine($"State changed: {result.PreviousState} -> {result.CurrentState}");
        }

        _log.WriteLine($"Dry-run iteration {result.Iteration} completed.");
    }
}
