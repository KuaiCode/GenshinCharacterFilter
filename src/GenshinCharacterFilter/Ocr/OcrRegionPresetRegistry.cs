using GenshinCharacterFilter.Calibration;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Stores supported OCR region preset names and real calibration-backed regions.
/// </summary>
public sealed class OcrRegionPresetRegistry
{
    /// <summary>
    /// Parses a user-facing preset name.
    /// </summary>
    public static OcrRegionPreset Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("OCR region preset cannot be empty.", nameof(value));
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "auto" => OcrRegionPreset.Auto,
            "2560x1600" => OcrRegionPreset.Size2560x1600,
            "1920x1080" => OcrRegionPreset.Size1920x1080,
            "none" => OcrRegionPreset.None,
            _ => throw new ArgumentException("OCR region preset must be auto, 2560x1600, 1920x1080, or none.", nameof(value))
        };
    }

    /// <summary>
    /// Returns the user-facing preset name.
    /// </summary>
    public static string GetDisplayName(OcrRegionPreset preset)
    {
        return preset switch
        {
            OcrRegionPreset.Auto => "auto",
            OcrRegionPreset.Size2560x1600 => "2560x1600",
            OcrRegionPreset.Size1920x1080 => "1920x1080",
            OcrRegionPreset.None => "none",
            _ => throw new ArgumentOutOfRangeException(nameof(preset), "Unknown OCR region preset.")
        };
    }

    /// <summary>
    /// Resolves auto preset by exact image size.
    /// </summary>
    public OcrRegionPreset? ResolveAuto(int imageWidth, int imageHeight)
    {
        return (imageWidth, imageHeight) switch
        {
            (2560, 1600) => OcrRegionPreset.Size2560x1600,
            (1920, 1080) => OcrRegionPreset.Size1920x1080,
            _ => null
        };
    }

    /// <summary>
    /// Returns a ratio region only when real calibration-backed preset data exists.
    /// </summary>
    public OcrRatioRegion? GetRatioRegion(OcrRegionPreset preset)
    {
        return preset switch
        {
            OcrRegionPreset.Size2560x1600 => null,
            OcrRegionPreset.Size1920x1080 => null,
            _ => null
        };
    }
}
