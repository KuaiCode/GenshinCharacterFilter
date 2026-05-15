using System.Drawing;
using System.Runtime.InteropServices;
using GenshinCharacterFilter.Calibration;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Resolves OCR regions from absolute pixels, calibration JSON, or named presets.
/// </summary>
public sealed class OcrRegionSourceResolver
{
    private readonly OcrRegionCalibrationFile _calibrationFile;
    private readonly OcrRegionPresetRegistry _presetRegistry;

    public OcrRegionSourceResolver(
        OcrRegionCalibrationFile? calibrationFile = null,
        OcrRegionPresetRegistry? presetRegistry = null)
    {
        _calibrationFile = calibrationFile ?? new OcrRegionCalibrationFile();
        _presetRegistry = presetRegistry ?? new OcrRegionPresetRegistry();
    }

    /// <summary>
    /// Resolves an OCR region for an input image path.
    /// </summary>
    public ResolvedOcrRegion ResolveForImage(OcrRegionSourceOptions options, string inputImagePath)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!options.HasEffectiveRegionSource)
        {
            return ResolvedOcrRegion.FullImage;
        }

        if (string.IsNullOrWhiteSpace(inputImagePath))
        {
            throw new ArgumentException("OCR input image path cannot be empty.", nameof(inputImagePath));
        }

        if (!File.Exists(inputImagePath))
        {
            throw new OcrRegionSourceException($"OCR input image was not found: {inputImagePath}");
        }

        try
        {
            using Image image = Image.FromFile(inputImagePath);
            return Resolve(options, image.Width, image.Height);
        }
        catch (OcrRegionSourceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is FileNotFoundException or IOException or UnauthorizedAccessException or OutOfMemoryException or ExternalException)
        {
            throw new OcrRegionSourceException($"Could not read OCR input image size from '{inputImagePath}': {exception.Message}", exception);
        }
    }

    /// <summary>
    /// Resolves an OCR region for a known image size.
    /// </summary>
    public ResolvedOcrRegion Resolve(OcrRegionSourceOptions options, int imageWidth, int imageHeight)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        ValidateImageSize(imageWidth, imageHeight);

        if (options.AbsoluteRegion is not null)
        {
            return ResolveAbsolute(options.AbsoluteRegion.Value, imageWidth, imageHeight);
        }

        if (!string.IsNullOrWhiteSpace(options.CalibrationFilePath))
        {
            return ResolveCalibrationFile(options.CalibrationFilePath, imageWidth, imageHeight);
        }

        if (options.Preset is not null)
        {
            return ResolvePreset(options.Preset.Value, imageWidth, imageHeight);
        }

        return ResolvedOcrRegion.FullImage;
    }

    private static ResolvedOcrRegion ResolveAbsolute(OcrRegion region, int imageWidth, int imageHeight)
    {
        try
        {
            region.ValidateWithin(imageWidth, imageHeight);
            return new ResolvedOcrRegion(region, "absolute");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new OcrRegionSourceException($"OCR absolute region {region} does not fit within image size {imageWidth}x{imageHeight}.", exception);
        }
    }

    private ResolvedOcrRegion ResolveCalibrationFile(string calibrationFilePath, int imageWidth, int imageHeight)
    {
        try
        {
            OcrRegionCalibrationResult calibration = _calibrationFile.Load(calibrationFilePath);
            OcrRegion region = ConvertRatioToPixel(calibration.RegionRatio, imageWidth, imageHeight);
            return new ResolvedOcrRegion(region, $"calibration file: {Path.GetFullPath(calibrationFilePath.Trim())}");
        }
        catch (OcrRegionSourceException)
        {
            throw;
        }
        catch (Exception exception) when (exception is CalibrationException or ArgumentException or ArgumentOutOfRangeException or IOException or UnauthorizedAccessException)
        {
            throw new OcrRegionSourceException($"Could not resolve OCR region from calibration file '{calibrationFilePath}': {exception.Message}", exception);
        }
    }

    private ResolvedOcrRegion ResolvePreset(OcrRegionPreset preset, int imageWidth, int imageHeight)
    {
        if (preset == OcrRegionPreset.None)
        {
            return ResolvedOcrRegion.FullImage;
        }

        OcrRegionPreset resolvedPreset = preset;
        if (preset == OcrRegionPreset.Auto)
        {
            OcrRegionPreset? autoPreset = _presetRegistry.ResolveAuto(imageWidth, imageHeight);
            if (autoPreset is null)
            {
                throw new OcrRegionSourceException($"No OCR region preset matches image size {imageWidth}x{imageHeight}; run --calibrate-ocr-region or provide --ocr-region-config.");
            }

            resolvedPreset = autoPreset.Value;
        }

        OcrRatioRegion? ratioRegion = _presetRegistry.GetRatioRegion(resolvedPreset);
        string presetName = OcrRegionPresetRegistry.GetDisplayName(resolvedPreset);
        if (ratioRegion is null)
        {
            throw new OcrRegionSourceException($"OCR region preset {presetName} is not configured yet; run --calibrate-ocr-region or provide --ocr-region-config.");
        }

        OcrRegion region = ConvertRatioToPixel(ratioRegion.Value, imageWidth, imageHeight);
        return new ResolvedOcrRegion(region, $"preset: {presetName}");
    }

    private static OcrRegion ConvertRatioToPixel(OcrRatioRegion ratioRegion, int imageWidth, int imageHeight)
    {
        ratioRegion.Validate();
        int x = RoundToInt(imageWidth * ratioRegion.X);
        int y = RoundToInt(imageHeight * ratioRegion.Y);
        int width = RoundToInt(imageWidth * ratioRegion.Width);
        int height = RoundToInt(imageHeight * ratioRegion.Height);
        OcrRegion region = new(x, y, width, height);

        try
        {
            region.ValidateWithin(imageWidth, imageHeight);
            return region;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new OcrRegionSourceException($"Resolved OCR region {region} does not fit within image size {imageWidth}x{imageHeight}.", exception);
        }
    }

    private static int RoundToInt(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static void ValidateImageSize(int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "OCR image width must be positive.");
        }

        if (imageHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageHeight), "OCR image height must be positive.");
        }
    }
}
