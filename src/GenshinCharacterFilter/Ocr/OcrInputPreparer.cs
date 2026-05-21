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
    public const string TempOcrDirectoryName = "GenshinCharacterFilter";

    /// <summary>
    /// Returns the original image path, a cropped debug image path, or a cropped temp image path.
    /// </summary>
    public string PrepareInput(OcrOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (options.OcrRegion is null && !options.HasNonDefaultPreparation())
        {
            return options.GetFullInputImagePath();
        }

        return PreparePreparedImage(options);
    }

    private static string PreparePreparedImage(OcrOptions options)
    {
        string inputImagePath = options.GetFullInputImagePath();
        string outputDirectory = options.SaveDebugImage
            ? Path.GetFullPath(DefaultDebugOutputDirectory)
            : GetTempOcrInputDirectory();
        Directory.CreateDirectory(outputDirectory);

        string debugOutputPath = Path.Combine(Path.GetFullPath(DefaultDebugOutputDirectory), DefaultDebugInputFileName);
        string outputPath = options.SaveDebugImage
            ? debugOutputPath
            : Path.Combine(outputDirectory, $"ocr-input-{Guid.NewGuid():N}.png");
        if (options.SaveDebugImage && string.Equals(inputImagePath, outputPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new OcrException(
                "OCR input image cannot be the debug OCR output image. Do not use debug-ocr/ocr-input-latest.png as OCR input; choose debug-captures/capture-latest.png or another original screenshot.");
        }

        try
        {
            byte[] inputBytes = File.ReadAllBytes(inputImagePath);
            using MemoryStream inputStream = new(inputBytes);
            using Image sourceImage = Image.FromStream(inputStream);
            using Bitmap preparedImage = PrepareBitmap(sourceImage, options);

            if (options.SaveDebugImage)
            {
                string tempPath = Path.Combine(
                    outputDirectory,
                    $"{Path.GetFileNameWithoutExtension(DefaultDebugInputFileName)}.{Guid.NewGuid():N}.tmp.png");
                preparedImage.Save(tempPath, ImageFormat.Png);
                MoveWithRetry(tempPath, outputPath);
            }
            else
            {
                preparedImage.Save(outputPath, ImageFormat.Png);
            }

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
        OcrRegion region = options.OcrRegion is null
            ? new OcrRegion(0, 0, sourceBitmap.Width, sourceBitmap.Height)
            : ExpandRegion(options.OcrRegion.Value, sourceBitmap.Width, sourceBitmap.Height, options.PaddingPixels);
        using Bitmap cropped = Crop(sourceBitmap, region);
        using Bitmap scaled = Scale(cropped, options.InputScale);
        using Bitmap transformed = ApplyPixelTransforms(scaled, options);
        return new Bitmap(transformed);
    }

    private static OcrRegion ExpandRegion(OcrRegion region, int imageWidth, int imageHeight, int paddingPixels)
    {
        if (paddingPixels == 0)
        {
            region.ValidateWithin(imageWidth, imageHeight);
            return region;
        }

        int left = Math.Max(0, region.X - paddingPixels);
        int top = Math.Max(0, region.Y - paddingPixels);
        int right = Math.Min(imageWidth, region.X + region.Width + paddingPixels);
        int bottom = Math.Min(imageHeight, region.Y + region.Height + paddingPixels);
        OcrRegion expanded = new(left, top, right - left, bottom - top);
        expanded.ValidateWithin(imageWidth, imageHeight);
        return expanded;
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

    private static Bitmap Scale(Bitmap source, int scale)
    {
        if (scale == OcrOptions.DefaultInputScale)
        {
            return new Bitmap(source);
        }

        Bitmap scaled = new(source.Width * scale, source.Height * scale);
        using Graphics graphics = Graphics.FromImage(scaled);
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        graphics.DrawImage(source, new Rectangle(0, 0, scaled.Width, scaled.Height));
        return scaled;
    }

    private static Bitmap ApplyPixelTransforms(Bitmap source, OcrOptions options)
    {
        if (!options.Grayscale && !options.Invert && options.Threshold is null)
        {
            return new Bitmap(source);
        }

        Bitmap transformed = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color pixel = source.GetPixel(x, y);
                int red = pixel.R;
                int green = pixel.G;
                int blue = pixel.B;

                if (options.Grayscale || options.Threshold is not null)
                {
                    int gray = (int)Math.Round(red * 0.299 + green * 0.587 + blue * 0.114);
                    red = gray;
                    green = gray;
                    blue = gray;
                }

                if (options.Threshold is not null)
                {
                    int value = red >= options.Threshold.Value ? 255 : 0;
                    red = value;
                    green = value;
                    blue = value;
                }

                if (options.Invert)
                {
                    red = 255 - red;
                    green = 255 - green;
                    blue = 255 - blue;
                }

                transformed.SetPixel(x, y, Color.FromArgb(pixel.A, red, green, blue));
            }
        }

        return transformed;
    }

    private static void MoveWithRetry(string tempPath, string outputPath)
    {
        const int maxAttempts = 5;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Move(tempPath, outputPath, overwrite: true);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts &&
                exception is IOException or UnauthorizedAccessException)
            {
                // A previewer or parallel test may briefly hold the old debug image.
                Thread.Sleep(50);
            }
        }

        File.Move(tempPath, outputPath, overwrite: true);
    }

    /// <summary>
    /// Returns the temp directory used for cropped OCR engine input during realtime loops.
    /// </summary>
    public static string GetTempOcrInputDirectory()
    {
        return Path.Combine(Path.GetTempPath(), TempOcrDirectoryName);
    }
}
