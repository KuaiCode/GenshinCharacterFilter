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
    public const int DefaultInputScale = 1;
    public const int MinInputScale = 1;
    public const int MaxInputScale = 4;
    public const int DefaultPaddingPixels = 0;
    public const int MaxPaddingPixels = 100;
    public const int MinThreshold = 0;
    public const int MaxThreshold = 255;

    /// <summary>
    /// Gets or sets the OCR engine to use.
    /// </summary>
    public OcrEngine OcrEngine { get; set; } = OcrEngine.TesseractCli;

    /// <summary>
    /// Gets or sets the tesseract executable path or command name.
    /// </summary>
    public string TesseractExecutablePath { get; set; } = DefaultTesseractExecutablePath;

    /// <summary>
    /// Gets or sets an optional PaddleOCR model directory.
    /// </summary>
    public string? PaddleModelDirectory { get; set; }

    /// <summary>
    /// Gets or sets an optional PaddleOCR native runtime directory.
    /// </summary>
    public string? PaddleRuntimeDirectory { get; set; }

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
    /// Gets or sets whether cropped OCR input should also be saved to the stable debug path.
    /// </summary>
    public bool SaveDebugImage { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional integer image scale applied after cropping.
    /// </summary>
    public int InputScale { get; set; } = DefaultInputScale;

    /// <summary>
    /// Gets or sets optional padding used to expand the OCR region before cropping.
    /// </summary>
    public int PaddingPixels { get; set; } = DefaultPaddingPixels;

    /// <summary>
    /// Gets or sets whether the prepared OCR input should be converted to grayscale.
    /// </summary>
    public bool Grayscale { get; set; }

    /// <summary>
    /// Gets or sets whether the prepared OCR input colors should be inverted.
    /// </summary>
    public bool Invert { get; set; }

    /// <summary>
    /// Gets or sets an optional binary threshold applied after grayscale conversion.
    /// </summary>
    public int? Threshold { get; set; }

    /// <summary>
    /// Validates options before invoking OCR.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(OcrEngine))
        {
            throw new ArgumentException("OCR engine is not supported.", nameof(OcrEngine));
        }

        if (OcrEngine == OcrEngine.TesseractCli &&
            string.IsNullOrWhiteSpace(TesseractExecutablePath))
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

        ValidatePreparationOptions(InputScale, PaddingPixels, Threshold);

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
    /// Returns true when OCR input preparation would alter the image even without region cropping.
    /// </summary>
    public bool HasNonDefaultPreparation()
    {
        return InputScale != DefaultInputScale ||
            PaddingPixels != DefaultPaddingPixels ||
            Grayscale ||
            Invert ||
            Threshold is not null;
    }

    public static void ValidatePreparationOptions(int inputScale, int paddingPixels, int? threshold)
    {
        if (inputScale is < MinInputScale or > MaxInputScale)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputScale),
                $"OCR input scale must be between {MinInputScale} and {MaxInputScale}.");
        }

        if (paddingPixels is < 0 or > MaxPaddingPixels)
        {
            throw new ArgumentOutOfRangeException(
                nameof(paddingPixels),
                $"OCR padding pixels must be between 0 and {MaxPaddingPixels}.");
        }

        if (threshold is < MinThreshold or > MaxThreshold)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                $"OCR threshold must be between {MinThreshold} and {MaxThreshold}, or omitted.");
        }
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
