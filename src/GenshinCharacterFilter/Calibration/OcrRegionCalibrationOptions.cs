using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Options for manual OCR region calibration.
/// </summary>
public sealed class OcrRegionCalibrationOptions
{
    public const string DefaultOutputPath = "ocr-region.json";

    public string TargetProcessName { get; init; } = "GenshinImpact";

    public string CaptureOutputDirectory { get; init; } = WindowCaptureOptions.DefaultOutputDirectory;

    public int CaptureDelayMs { get; init; } = WindowCaptureOptions.DefaultCaptureDelayMs;

    public string CalibrationOutputPath { get; init; } = DefaultOutputPath;

    /// <summary>
    /// Validates calibration options before capture starts.
    /// </summary>
    public void Validate()
    {
        WindowCaptureOptions.NormalizeProcessName(TargetProcessName);

        if (string.IsNullOrWhiteSpace(CaptureOutputDirectory))
        {
            throw new ArgumentException("Capture output directory cannot be empty.", nameof(CaptureOutputDirectory));
        }

        if (string.IsNullOrWhiteSpace(CalibrationOutputPath))
        {
            throw new ArgumentException("Calibration output path cannot be empty.", nameof(CalibrationOutputPath));
        }

        WindowCaptureOptions.ValidateCaptureDelayMs(CaptureDelayMs);
    }

    /// <summary>
    /// Creates capture options for the calibration screenshot.
    /// </summary>
    public WindowCaptureOptions ToWindowCaptureOptions()
    {
        Validate();

        return new WindowCaptureOptions
        {
            TargetProcessName = TargetProcessName,
            OutputDirectory = CaptureOutputDirectory,
            CaptureDelayMs = CaptureDelayMs
        };
    }

    /// <summary>
    /// Returns the absolute output path for calibration JSON.
    /// </summary>
    public string GetCalibrationOutputPath()
    {
        Validate();
        return Path.GetFullPath(CalibrationOutputPath.Trim());
    }
}
