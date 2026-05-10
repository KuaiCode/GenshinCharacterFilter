namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Contains raw OCR output and minimal metadata for a one-shot OCR run.
/// </summary>
public sealed class OcrResult
{
    public OcrResult(string rawText, string engineName, string inputImagePath)
    {
        RawText = rawText ?? string.Empty;
        NormalizedText = RawText.Trim();
        EngineName = string.IsNullOrWhiteSpace(engineName)
            ? throw new ArgumentException("Engine name cannot be empty.", nameof(engineName))
            : engineName;
        InputImagePath = string.IsNullOrWhiteSpace(inputImagePath)
            ? throw new ArgumentException("Input image path cannot be empty.", nameof(inputImagePath))
            : inputImagePath;
    }

    /// <summary>
    /// Gets the raw OCR text exactly as returned by the provider.
    /// </summary>
    public string RawText { get; }

    /// <summary>
    /// Gets the raw OCR text after a simple trim.
    /// </summary>
    public string NormalizedText { get; }

    /// <summary>
    /// Gets the OCR provider name.
    /// </summary>
    public string EngineName { get; }

    /// <summary>
    /// Gets the local image path used for OCR.
    /// </summary>
    public string InputImagePath { get; }
}
