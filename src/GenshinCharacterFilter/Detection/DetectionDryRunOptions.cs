using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Options for the explicit OCR-driven detection dry-run loop.
/// </summary>
public sealed class DetectionDryRunOptions
{
    public const int DefaultLoopIntervalMs = 1000;
    public const int MinLoopIntervalMs = 100;
    public const int MaxLoopIntervalMs = 10000;

    /// <summary>
    /// Gets or sets the optional fixed image path used for repeated OCR.
    /// </summary>
    public string? OcrInputPath { get; set; }

    /// <summary>
    /// Gets or sets the target process name used when the loop captures a window each iteration.
    /// </summary>
    public string? TargetProcessName { get; set; }

    /// <summary>
    /// Gets or sets the optional OCR region in image coordinates.
    /// </summary>
    public OcrRegion? OcrRegion { get; set; }

    /// <summary>
    /// Gets or sets an optional calibration JSON path used to resolve OCR region.
    /// </summary>
    public string? OcrRegionConfigPath { get; set; }

    /// <summary>
    /// Gets or sets an optional built-in OCR region preset selector.
    /// </summary>
    public OcrRegionPreset? OcrRegionPreset { get; set; }

    /// <summary>
    /// Gets or sets the OCR language expression passed to Tesseract.
    /// </summary>
    public string OcrLanguage { get; set; } = OcrOptions.DefaultLanguage;

    /// <summary>
    /// Gets or sets the Tesseract executable path or command name.
    /// </summary>
    public string TesseractExecutablePath { get; set; } = OcrOptions.DefaultTesseractExecutablePath;

    /// <summary>
    /// Gets or sets the Tesseract page segmentation mode.
    /// </summary>
    public int OcrPageSegmentationMode { get; set; } = OcrOptions.DefaultPageSegmentationMode;

    /// <summary>
    /// Gets or sets the configured target speaker names.
    /// </summary>
    public IReadOnlyCollection<string> TargetSpeakers { get; set; } = [];

    /// <summary>
    /// Gets or sets the delay between loop iterations.
    /// </summary>
    public int LoopIntervalMs { get; set; } = DefaultLoopIntervalMs;

    /// <summary>
    /// Gets or sets the optional fixed iteration count. Null means run until cancellation.
    /// </summary>
    public int? LoopCount { get; set; }

    /// <summary>
    /// Gets or sets the capture output directory when process capture mode is used.
    /// </summary>
    public string CaptureOutputDirectory { get; set; } = WindowCaptureOptions.DefaultOutputDirectory;

    /// <summary>
    /// Gets or sets the capture foreground activation delay when process capture mode is used.
    /// </summary>
    public int CaptureDelayMs { get; set; } = WindowCaptureOptions.DefaultCaptureDelayMs;

    /// <summary>
    /// Gets or sets the stability gate options used by the dry-run loop.
    /// </summary>
    public DetectionStabilityOptions Stability { get; set; } = new();

    /// <summary>
    /// Validates dry-run options before the loop starts.
    /// </summary>
    public void Validate()
    {
        ValidateLoopIntervalMs(LoopIntervalMs);
        ValidateLoopCount(LoopCount);
        Stability.Validate();

        bool hasFixedImage = !string.IsNullOrWhiteSpace(OcrInputPath);
        bool hasProcess = !string.IsNullOrWhiteSpace(TargetProcessName);
        if (!hasFixedImage && !hasProcess)
        {
            throw new ArgumentException("Detect loop requires --ocr-input <imagePath> or --process <name>.");
        }

        if (string.IsNullOrWhiteSpace(OcrLanguage))
        {
            throw new ArgumentException("OCR language cannot be empty.", nameof(OcrLanguage));
        }

        if (string.IsNullOrWhiteSpace(TesseractExecutablePath))
        {
            throw new ArgumentException("Tesseract executable path cannot be empty.", nameof(TesseractExecutablePath));
        }

        if (OcrPageSegmentationMode is < OcrOptions.MinPageSegmentationMode or > OcrOptions.MaxPageSegmentationMode)
        {
            throw new ArgumentOutOfRangeException(
                nameof(OcrPageSegmentationMode),
                $"OCR page segmentation mode must be between {OcrOptions.MinPageSegmentationMode} and {OcrOptions.MaxPageSegmentationMode}.");
        }

        OcrRegion?.ValidateShape();
        GetOcrRegionSourceOptions().Validate();
    }

    /// <summary>
    /// Builds OCR region source options for the current loop.
    /// </summary>
    public OcrRegionSourceOptions GetOcrRegionSourceOptions()
    {
        return new OcrRegionSourceOptions
        {
            AbsoluteRegion = OcrRegion,
            CalibrationFilePath = OcrRegionConfigPath,
            Preset = OcrRegionPreset
        };
    }

    /// <summary>
    /// Validates the loop interval range.
    /// </summary>
    public static void ValidateLoopIntervalMs(int loopIntervalMs)
    {
        if (loopIntervalMs is < MinLoopIntervalMs or > MaxLoopIntervalMs)
        {
            throw new ArgumentOutOfRangeException(
                nameof(loopIntervalMs),
                $"Loop interval must be between {MinLoopIntervalMs} and {MaxLoopIntervalMs} ms.");
        }
    }

    /// <summary>
    /// Validates the optional loop count.
    /// </summary>
    public static void ValidateLoopCount(int? loopCount)
    {
        if (loopCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(loopCount), "Loop count must be greater than 0.");
        }
    }
}
