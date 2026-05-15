using GenshinCharacterFilter.Calibration;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrRegionCalibrationTests
{
    [Fact]
    public void FromPixelRegion_ComputesRatioRegion()
    {
        OcrRegionCalibrationResult result = OcrRegionCalibrationResult.FromPixelRegion(
            1000,
            500,
            new OcrRegion(100, 50, 400, 100),
            "notepad",
            new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(0.1, result.RegionRatio.X, 6);
        Assert.Equal(0.1, result.RegionRatio.Y, 6);
        Assert.Equal(0.4, result.RegionRatio.Width, 6);
        Assert.Equal(0.2, result.RegionRatio.Height, 6);
        Assert.Equal("notepad", result.SourceProcessName);
    }

    [Fact]
    public void RatioRegion_ToPixelRegion_ComputesPixelRegion()
    {
        OcrRegion region = new OcrRatioRegion(0.1, 0.2, 0.3, 0.4).ToPixelRegion(1000, 500);

        Assert.Equal(new OcrRegion(100, 100, 300, 200), region);
    }

    [Fact]
    public void FromPixelRegion_RejectsRegionOutsideImage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OcrRegionCalibrationResult.FromPixelRegion(1000, 500, new OcrRegion(900, 100, 200, 50)));
    }

    [Fact]
    public void RatioRegion_RejectsRegionOutsideImage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new OcrRatioRegion(0.9, 0.1, 0.2, 0.2).Validate());
    }

    [Fact]
    public void CalibrationFile_SavesAndLoadsUtf8Json()
    {
        string directory = Path.Combine(Path.GetTempPath(), "GenshinCharacterFilterTests", Guid.NewGuid().ToString("N"));
        string outputPath = Path.Combine(directory, "ocr-region.json");
        OcrRegionCalibrationResult result = OcrRegionCalibrationResult.FromPixelRegion(
            800,
            600,
            new OcrRegion(80, 120, 240, 60),
            "notepad",
            new DateTimeOffset(2026, 5, 15, 1, 2, 3, TimeSpan.Zero));
        OcrRegionCalibrationFile file = new();

        try
        {
            file.Save(result, outputPath);

            string json = File.ReadAllText(outputPath);
            Assert.Contains("\"sourceImageWidth\": 800", json);
            Assert.Contains("\"regionPixels\"", json);

            OcrRegionCalibrationResult loaded = file.Load(outputPath);

            Assert.Equal(800, loaded.SourceImageWidth);
            Assert.Equal(600, loaded.SourceImageHeight);
            Assert.Equal(new OcrRegion(80, 120, 240, 60), loaded.RegionPixels);
            Assert.Equal("notepad", loaded.SourceProcessName);
            Assert.Equal(0.1, loaded.RegionRatio.X, 6);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void CalibrationFile_LoadRejectsMissingFile()
    {
        OcrRegionCalibrationFile file = new();

        CalibrationException exception = Assert.Throws<CalibrationException>(
            () => file.Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json")));

        Assert.Contains("was not found", exception.Message);
    }
}
