namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Represents a user-facing OCR region calibration error.
/// </summary>
public sealed class CalibrationException : Exception
{
    public CalibrationException(string message)
        : base(message)
    {
    }

    public CalibrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
