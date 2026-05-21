using System.Diagnostics;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Runs repeated OCR against one prepared crop so OCR engines can be compared.
/// </summary>
public sealed class OcrBenchmarkRunner
{
    private readonly IOcrService _ocrService;
    private readonly OcrInputPreparer _inputPreparer;

    public OcrBenchmarkRunner(IOcrService ocrService, OcrInputPreparer? inputPreparer = null)
    {
        _ocrService = ocrService;
        _inputPreparer = inputPreparer ?? new OcrInputPreparer();
    }

    public async Task<OcrBenchmarkResult> RunAsync(
        OcrOptions options,
        int repeat,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (repeat <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repeat), "OCR repeat must be greater than 0.");
        }

        string preparedInputPath = _inputPreparer.PrepareInput(options);
        OcrOptions preparedOptions = new()
        {
            OcrEngine = options.OcrEngine,
            TesseractExecutablePath = options.TesseractExecutablePath,
            PaddleModelDirectory = options.PaddleModelDirectory,
            PaddleRuntimeDirectory = options.PaddleRuntimeDirectory,
            Language = options.Language,
            PageSegmentationMode = options.PageSegmentationMode,
            InputImagePath = preparedInputPath,
            InputScale = OcrOptions.DefaultInputScale,
            PaddingPixels = OcrOptions.DefaultPaddingPixels
        };

        List<OcrBenchmarkRunResult> runs = [];
        int failureCount = 0;
        for (int i = 1; i <= repeat; i++)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                OcrResult result = await _ocrService.ExtractTextAsync(preparedOptions, cancellationToken);
                stopwatch.Stop();
                runs.Add(new OcrBenchmarkRunResult(i, result.RawText, stopwatch.ElapsedMilliseconds, null));
            }
            catch (Exception exception) when (exception is OcrException or FileNotFoundException or ArgumentException)
            {
                stopwatch.Stop();
                failureCount++;
                runs.Add(new OcrBenchmarkRunResult(i, string.Empty, stopwatch.ElapsedMilliseconds, exception.Message));
            }
        }

        return new OcrBenchmarkResult(options.OcrEngine, preparedInputPath, repeat, failureCount, runs);
    }
}

public sealed record OcrBenchmarkResult(
    OcrEngine Engine,
    string PreparedInputPath,
    int Repeat,
    int FailureCount,
    IReadOnlyList<OcrBenchmarkRunResult> Runs)
{
    public long FirstRunElapsedMs => Runs.Count == 0 ? 0 : Runs[0].ElapsedMs;

    public double AverageElapsedMs => Runs.Count == 0 ? 0 : Runs.Average(run => run.ElapsedMs);

    public double WarmAverageElapsedMs => Runs.Count <= 1 ? 0 : Runs.Skip(1).Average(run => run.ElapsedMs);

    public long MinElapsedMs => Runs.Count == 0 ? 0 : Runs.Min(run => run.ElapsedMs);

    public long MaxElapsedMs => Runs.Count == 0 ? 0 : Runs.Max(run => run.ElapsedMs);
}

public sealed record OcrBenchmarkRunResult(
    int Iteration,
    string RawText,
    long ElapsedMs,
    string? Error);
