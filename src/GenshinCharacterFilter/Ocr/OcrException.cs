namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Represents a recoverable OCR failure with a user-facing message.
/// </summary>
public sealed class OcrException : Exception
{
    public OcrException(string message)
        : base(message)
    {
    }

    public OcrException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
