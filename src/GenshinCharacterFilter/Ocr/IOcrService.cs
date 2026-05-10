namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Extracts raw text from a local image.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Runs OCR once and returns the raw extracted text.
    /// </summary>
    Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken);
}
