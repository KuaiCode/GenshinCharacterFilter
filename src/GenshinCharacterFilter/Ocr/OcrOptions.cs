namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Options for explicit one-shot OCR text extraction.
/// </summary>
public sealed class OcrOptions
{
    public const string DefaultTesseractExecutablePath = "tesseract";
    public const string DefaultLanguage = "chi_sim+eng";
    public const int DefaultPageSegmentationMode = 7;
    public const int MinPageSegmentationMode = 0;
    public const int MaxPageSegmentationMode = 13;

    /// <summary>
    /// Gets or sets the OCR engine to use.
    /// </summary>
    public OcrEngine OcrEngine { get; set; } = OcrEngine.TesseractCli;

    /// <summary>
    /// Gets or sets the tesseract executable path or command name.
    /// </summary>
    public string TesseractExecutablePath { get; set; } = DefaultTesseractExecutablePath;

    /// <summary>
    /// Gets or sets the OCR language expression passed to tesseract.
    /// </summary>
    public string Language { get; set; } = DefaultLanguage;

    /// <summary>
    /// Gets or sets the tesseract page segmentation mode.
    /// </summary>
    public int PageSegmentationMode { get; set; } = DefaultPageSegmentationMode;

    /// <summary>
    /// Gets or sets the local image path used as OCR input.
    /// </summary>
    public string? InputImagePath { get; set; }

    /// <summary>
    /// Gets or sets an optional region in input image coordinates.
    /// </summary>
    public OcrRegion? OcrRegion { get; set; }

    /// <summary>
    /// Validates options before invoking OCR.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(OcrEngine))
        {
            throw new ArgumentException("OCR engine is not supported.", nameof(OcrEngine));
        }

        if (string.IsNullOrWhiteSpace(TesseractExecutablePath))
        {
            throw new ArgumentException("Tesseract executable path cannot be empty.", nameof(TesseractExecutablePath));
        }

        if (string.IsNullOrWhiteSpace(Language))
        {
            throw new ArgumentException("OCR language cannot be empty.", nameof(Language));
        }

        if (PageSegmentationMode is < MinPageSegmentationMode or > MaxPageSegmentationMode)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PageSegmentationMode),
                $"OCR page segmentation mode must be between {MinPageSegmentationMode} and {MaxPageSegmentationMode}.");
        }

        if (string.IsNullOrWhiteSpace(InputImagePath))
        {
            throw new ArgumentException("OCR input image path cannot be empty.", nameof(InputImagePath));
        }

        if (!File.Exists(InputImagePath))
        {
            throw new FileNotFoundException($"OCR input image was not found: {InputImagePath}", InputImagePath);
        }

        OcrRegion?.ValidateShape();
    }

    /// <summary>
    /// Returns the absolute OCR input image path.
    /// </summary>
    public string GetFullInputImagePath()
    {
        Validate();
        return Path.GetFullPath(InputImagePath!);
    }
}
