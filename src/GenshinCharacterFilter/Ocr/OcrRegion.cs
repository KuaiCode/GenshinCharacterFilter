namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Describes a rectangular OCR region in input image coordinates.
/// </summary>
public readonly record struct OcrRegion(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Parses an OCR region in x,y,width,height format.
    /// </summary>
    public static OcrRegion Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("OCR region cannot be empty.", nameof(value));
        }

        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 4)
        {
            throw new ArgumentException("OCR region must use x,y,width,height format.", nameof(value));
        }

        if (!int.TryParse(parts[0], out int x) ||
            !int.TryParse(parts[1], out int y) ||
            !int.TryParse(parts[2], out int width) ||
            !int.TryParse(parts[3], out int height))
        {
            throw new ArgumentException("OCR region values must be numbers.", nameof(value));
        }

        OcrRegion region = new(x, y, width, height);
        region.ValidateShape();
        return region;
    }

    /// <summary>
    /// Validates region coordinates and dimensions without image bounds.
    /// </summary>
    public void ValidateShape()
    {
        if (X < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(X), "OCR region x must be greater than or equal to 0.");
        }

        if (Y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Y), "OCR region y must be greater than or equal to 0.");
        }

        if (Width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "OCR region width must be greater than 0.");
        }

        if (Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Height), "OCR region height must be greater than 0.");
        }
    }

    /// <summary>
    /// Validates that this region fits inside an input image.
    /// </summary>
    public void ValidateWithin(int imageWidth, int imageHeight)
    {
        if (imageWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageWidth), "OCR input image width must be positive.");
        }

        if (imageHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(imageHeight), "OCR input image height must be positive.");
        }

        ValidateShape();

        if ((long)X + Width > imageWidth || (long)Y + Height > imageHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(Width), "OCR region must fit within the input image.");
        }
    }
}
