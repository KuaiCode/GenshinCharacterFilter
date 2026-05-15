using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Stores the selected OCR region in pixel and ratio coordinates.
/// </summary>
public sealed class OcrRegionCalibrationResult
{
    public int SourceImageWidth { get; init; }

    public int SourceImageHeight { get; init; }

    public OcrRegion RegionPixels { get; init; }

    public OcrRatioRegion RegionRatio { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    public string? SourceProcessName { get; init; }

    /// <summary>
    /// Creates a calibration result from a selected pixel region.
    /// </summary>
    public static OcrRegionCalibrationResult FromPixelRegion(
        int imageWidth,
        int imageHeight,
        OcrRegion regionPixels,
        string? sourceProcessName = null,
        DateTimeOffset? generatedAt = null)
    {
        OcrRatioRegion.ValidateImageSize(imageWidth, imageHeight);
        regionPixels.ValidateWithin(imageWidth, imageHeight);

        OcrRegionCalibrationResult result = new()
        {
            SourceImageWidth = imageWidth,
            SourceImageHeight = imageHeight,
            RegionPixels = regionPixels,
            RegionRatio = new OcrRatioRegion(
                regionPixels.X / (double)imageWidth,
                regionPixels.Y / (double)imageHeight,
                regionPixels.Width / (double)imageWidth,
                regionPixels.Height / (double)imageHeight),
            GeneratedAt = generatedAt ?? DateTimeOffset.UtcNow,
            SourceProcessName = string.IsNullOrWhiteSpace(sourceProcessName) ? null : sourceProcessName.Trim()
        };

        result.Validate();
        return result;
    }

    /// <summary>
    /// Creates a calibration result from a ratio region.
    /// </summary>
    public static OcrRegionCalibrationResult FromRatioRegion(
        int imageWidth,
        int imageHeight,
        OcrRatioRegion regionRatio,
        string? sourceProcessName = null,
        DateTimeOffset? generatedAt = null)
    {
        OcrRegion regionPixels = regionRatio.ToPixelRegion(imageWidth, imageHeight);
        return FromPixelRegion(imageWidth, imageHeight, regionPixels, sourceProcessName, generatedAt);
    }

    /// <summary>
    /// Validates stored image dimensions and OCR regions.
    /// </summary>
    public void Validate()
    {
        OcrRatioRegion.ValidateImageSize(SourceImageWidth, SourceImageHeight);
        RegionPixels.ValidateWithin(SourceImageWidth, SourceImageHeight);
        RegionRatio.Validate();
    }
}
