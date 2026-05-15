namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Represents an OCR region source resolution error.
/// </summary>
public sealed class OcrRegionSourceException : Exception
{
    public OcrRegionSourceException(string message)
        : base(message)
    {
    }

    public OcrRegionSourceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
