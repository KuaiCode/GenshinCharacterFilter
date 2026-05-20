using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Saves a cropped preview of the selected OCR calibration region for manual inspection.
/// </summary>
public sealed class CalibrationRegionPreviewSaver
{
    public const string DefaultPreviewDirectory = "debug-ocr";
    public const string DefaultPreviewFileName = "calibration-region-latest.png";

    public string SavePreview(string screenshotPath, OcrRegion region, string? outputPath = null)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
        {
            throw new ArgumentException("Calibration screenshot path cannot be empty.", nameof(screenshotPath));
        }

        string fullScreenshotPath = Path.GetFullPath(screenshotPath);
        if (!File.Exists(fullScreenshotPath))
        {
            throw new FileNotFoundException($"Calibration screenshot was not found: {fullScreenshotPath}", fullScreenshotPath);
        }

        string previewPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetFullPath(DefaultPreviewDirectory), DefaultPreviewFileName)
            : Path.GetFullPath(outputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);

        try
        {
            byte[] screenshotBytes = File.ReadAllBytes(fullScreenshotPath);
            using MemoryStream screenshotStream = new(screenshotBytes);
            using Image sourceImage = Image.FromStream(screenshotStream);
            using Bitmap sourceBitmap = new(sourceImage);
            return SavePreview(sourceBitmap, region, previewPath);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or OutOfMemoryException or ExternalException)
        {
            throw new CalibrationException($"Could not save calibration region preview: {exception.Message}", exception);
        }
    }

    public string SavePreview(Bitmap sourceBitmap, OcrRegion region, string? outputPath = null)
    {
        ArgumentNullException.ThrowIfNull(sourceBitmap);

        string previewPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetFullPath(DefaultPreviewDirectory), DefaultPreviewFileName)
            : Path.GetFullPath(outputPath);

        Directory.CreateDirectory(Path.GetDirectoryName(previewPath)!);
        region.ValidateWithin(sourceBitmap.Width, sourceBitmap.Height);

        using Bitmap preview = new(region.Width, region.Height);
        using (Graphics graphics = Graphics.FromImage(preview))
        {
            graphics.DrawImage(
                sourceBitmap,
                new Rectangle(0, 0, region.Width, region.Height),
                new Rectangle(region.X, region.Y, region.Width, region.Height),
                GraphicsUnit.Pixel);
        }

        string tempPath = Path.Combine(
            Path.GetDirectoryName(previewPath)!,
            $"{Path.GetFileNameWithoutExtension(previewPath)}.{Guid.NewGuid():N}.tmp.png");
        preview.Save(tempPath, ImageFormat.Png);
        File.Move(tempPath, previewPath, overwrite: true);
        return previewPath;
    }
}
