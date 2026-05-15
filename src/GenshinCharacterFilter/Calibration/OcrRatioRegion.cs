using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Describes an OCR region as ratios relative to source image size.
/// </summary>
public readonly record struct OcrRatioRegion(double X, double Y, double Width, double Height)
{
    /// <summary>
    /// Validates ratio coordinates and dimensions.
    /// </summary>
    public void Validate()
    {
        ValidateFinite(X, nameof(X));
        ValidateFinite(Y, nameof(Y));
        ValidateFinite(Width, nameof(Width));
        ValidateFinite(Height, nameof(Height));

        if (X < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(X), "OCR ratio region x must be greater than or equal to 0.");
        }

        if (Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Y), "OCR ratio region y must be greater than or equal to 0.");
        }

        if (Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "OCR ratio region width must be greater than 0.");
        }

        if (Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), "OCR ratio region height must be greater than 0.");
        }

        if (X + Width > 1.0 || Y + Height > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "OCR ratio region must fit inside the source image.");
        }
    }

    /// <summary>
    /// Converts this ratio region into pixel coordinates for a source image.
    /// </summary>
    public OcrRegion ToPixelRegion(int imageWidth, int imageHeight)
    {
        ValidateImageSize(imageWidth, imageHeight);
        Validate();

        int x = ClampToRange(RoundToInt(X * imageWidth), 0, imageWidth - 1);
        int y = ClampToRange(RoundToInt(Y * imageHeight), 0, imageHeight - 1);

        // 比例反算像素时做边界收敛，避免浮点舍入让区域越过图片边界。
        int width = ClampToRange(RoundToInt(Width * imageWidth), 1, imageWidth - x);
        int height = ClampToRange(RoundToInt(Height * imageHeight), 1, imageHeight - y);

        OcrRegion region = new(x, y, width, height);
        region.ValidateWithin(imageWidth, imageHeight);
        return region;
    }

    internal static void ValidateImageSize(int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "Source image width must be positive.");
        }

        if (imageHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageHeight), "Source image height must be positive.");
        }
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "OCR ratio region values must be finite numbers.");
        }
    }

    private static int RoundToInt(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }

    private static int ClampToRange(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }
}
