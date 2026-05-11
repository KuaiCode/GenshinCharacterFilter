using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Prepares the local image passed to OCR, including optional region cropping.
/// </summary>
public sealed class OcrInputPreparer
{
    public const string DefaultDebugOutputDirectory = "debug-ocr";
    public const string DefaultDebugInputFileName = "ocr-input-latest.png";

    /// <summary>
    /// Returns the original image path or a cropped debug image path when an OCR region is configured.
    /// </summary>
    public string PrepareInput(OcrOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (options.OcrRegion is null)
        {
            return options.GetFullInputImagePath();
        }

        return PrepareDebugImage(options);
    }

    private static string PrepareDebugImage(OcrOptions options)
    {
        string inputImagePath = options.GetFullInputImagePath();

        try
        {
            using Image sourceImage = Image.FromFile(inputImagePath);
            using Bitmap preparedImage = PrepareBitmap(sourceImage, options);

            string outputDirectory = Path.GetFullPath(DefaultDebugOutputDirectory);
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, DefaultDebugInputFileName);
            preparedImage.Save(outputPath, ImageFormat.Png);
            return outputPath;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new OcrException($"OCR region does not fit within input image '{inputImagePath}'. {exception.Message}", exception);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OutOfMemoryException or ExternalException)
        {
            throw new OcrException($"Could not prepare OCR input image '{inputImagePath}': {exception.Message}", exception);
        }
    }

    private static Bitmap PrepareBitmap(Image sourceImage, OcrOptions options)
    {
        using Bitmap sourceBitmap = new(sourceImage);
        return Crop(sourceBitmap, options.OcrRegion!.Value);
    }

    private static Bitmap Crop(Bitmap source, OcrRegion region)
    {
        region.ValidateWithin(source.Width, source.Height);

        Bitmap cropped = new(region.Width, region.Height);
        using Graphics graphics = Graphics.FromImage(cropped);
        graphics.DrawImage(
            source,
            new Rectangle(0, 0, region.Width, region.Height),
            new Rectangle(region.X, region.Y, region.Width, region.Height),
            GraphicsUnit.Pixel);

        return cropped;
    }
}
