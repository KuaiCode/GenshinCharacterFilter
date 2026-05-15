using System.Drawing;
using GenshinCharacterFilter.Calibration;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrRegionSourceResolverTests
{
    [Fact]
    public void Resolve_WithNoRegionSourceUsesFullImage()
    {
        ResolvedOcrRegion resolved = new OcrRegionSourceResolver().Resolve(new OcrRegionSourceOptions(), 1900, 1082);

        Assert.Null(resolved.Region);
        Assert.Equal("none/full image", resolved.SourceLabel);
    }

    [Fact]
    public void Resolve_WithAbsoluteRegionPreservesPixelRegion()
    {
        ResolvedOcrRegion resolved = new OcrRegionSourceResolver().Resolve(
            new OcrRegionSourceOptions
            {
                AbsoluteRegion = new OcrRegion(10, 20, 30, 40)
            },
            100,
            100);

        Assert.Equal(new OcrRegion(10, 20, 30, 40), resolved.Region);
        Assert.Equal("absolute", resolved.SourceLabel);
    }

    [Fact]
    public void Resolve_WithCalibrationFileUsesRatioForCurrentImageSize()
    {
        using TempCalibrationFile calibration = TempCalibrationFile.Create(
            new OcrRatioRegion(0.014737, 0.343808, 0.088421, 0.040665));

        ResolvedOcrRegion resolved = new OcrRegionSourceResolver().Resolve(
            new OcrRegionSourceOptions
            {
                CalibrationFilePath = calibration.Path
            },
            1900,
            1082);

        Assert.Equal(new OcrRegion(28, 372, 168, 44), resolved.Region);
        Assert.Contains("calibration file", resolved.SourceLabel);
    }

    [Fact]
    public void Resolve_WithCalibrationFileScalesForDifferentImageSizes()
    {
        using TempCalibrationFile calibration = TempCalibrationFile.Create(
            new OcrRatioRegion(0.1, 0.2, 0.3, 0.4));
        OcrRegionSourceResolver resolver = new();
        OcrRegionSourceOptions options = new()
        {
            CalibrationFilePath = calibration.Path
        };

        ResolvedOcrRegion small = resolver.Resolve(options, 200, 100);
        ResolvedOcrRegion large = resolver.Resolve(options, 1000, 500);

        Assert.Equal(new OcrRegion(20, 20, 60, 40), small.Region);
        Assert.Equal(new OcrRegion(100, 100, 300, 200), large.Region);
    }

    [Fact]
    public void Resolve_WithMissingCalibrationFileThrowsClearError()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "ocr-region.json");

        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { CalibrationFilePath = missingPath },
                100,
                100));

        Assert.Contains("calibration file", exception.Message);
        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public void Resolve_WithInvalidCalibrationJsonThrowsClearError()
    {
        using TempTextFile calibration = TempTextFile.Create("{ not json");

        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { CalibrationFilePath = calibration.Path },
                100,
                100));

        Assert.Contains("not valid JSON", exception.Message);
    }

    [Fact]
    public void Resolve_WithInvalidRatioRegionThrowsClearError()
    {
        string json = """
            {
              "sourceImageWidth": 100,
              "sourceImageHeight": 100,
              "regionPixels": { "x": 10, "y": 10, "width": 10, "height": 10 },
              "regionRatio": { "x": 0.9, "y": 0.1, "width": 0.2, "height": 0.2 },
              "generatedAt": "2026-05-15T00:00:00+00:00",
              "sourceProcessName": "notepad"
            }
            """;
        using TempTextFile calibration = TempTextFile.Create(json);

        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { CalibrationFilePath = calibration.Path },
                100,
                100));

        Assert.Contains("calibration file", exception.Message);
    }

    [Fact]
    public void Resolve_RejectsOutOfBoundsAbsoluteRegion()
    {
        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { AbsoluteRegion = new OcrRegion(90, 0, 20, 10) },
                100,
                100));

        Assert.Contains("does not fit", exception.Message);
    }

    [Fact]
    public void ResolveForImage_UsesResolvedRegionWithOcrInputPreparer()
    {
        using TempImage image = TempImage.Create(100, 100);
        using TempCalibrationFile calibration = TempCalibrationFile.Create(
            new OcrRatioRegion(0.1, 0.2, 0.3, 0.4));
        OcrRegionSourceResolver resolver = new();
        ResolvedOcrRegion resolved = resolver.ResolveForImage(
            new OcrRegionSourceOptions { CalibrationFilePath = calibration.Path },
            image.Path);
        OcrOptions options = new()
        {
            InputImagePath = image.Path,
            OcrRegion = resolved.Region
        };

        string preparedPath = new OcrInputPreparer().PrepareInput(options);

        using Image cropped = Image.FromFile(preparedPath);
        Assert.Equal(30, cropped.Width);
        Assert.Equal(40, cropped.Height);
    }

    [Theory]
    [InlineData("auto", OcrRegionPreset.Auto)]
    [InlineData("2560x1600", OcrRegionPreset.Size2560x1600)]
    [InlineData("1920x1080", OcrRegionPreset.Size1920x1080)]
    [InlineData("none", OcrRegionPreset.None)]
    public void PresetRegistry_ParseAcceptsKnownNames(string name, OcrRegionPreset expected)
    {
        Assert.Equal(expected, OcrRegionPresetRegistry.Parse(name));
    }

    [Fact]
    public void PresetRegistry_ParseRejectsUnknownName()
    {
        Assert.Throws<ArgumentException>(() => OcrRegionPresetRegistry.Parse("4k"));
    }

    [Fact]
    public void PresetRegistry_AutoMatchesSupportedImageSizes()
    {
        OcrRegionPresetRegistry registry = new();

        Assert.Equal(OcrRegionPreset.Size2560x1600, registry.ResolveAuto(2560, 1600));
        Assert.Equal(OcrRegionPreset.Size1920x1080, registry.ResolveAuto(1920, 1080));
        Assert.Null(registry.ResolveAuto(1900, 1082));
    }

    [Fact]
    public void Resolve_AutoPresetForUnknownSizeRequiresCalibration()
    {
        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { Preset = OcrRegionPreset.Auto },
                1900,
                1082));

        Assert.Contains("No OCR region preset matches image size 1900x1082", exception.Message);
    }

    [Theory]
    [InlineData(OcrRegionPreset.Auto, 2560, 1600, "2560x1600")]
    [InlineData(OcrRegionPreset.Auto, 1920, 1080, "1920x1080")]
    [InlineData(OcrRegionPreset.Size2560x1600, 2560, 1600, "2560x1600")]
    public void Resolve_UnconfiguredPresetThrowsClearError(
        OcrRegionPreset preset,
        int width,
        int height,
        string expectedPresetName)
    {
        OcrRegionSourceException exception = Assert.Throws<OcrRegionSourceException>(
            () => new OcrRegionSourceResolver().Resolve(
                new OcrRegionSourceOptions { Preset = preset },
                width,
                height));

        Assert.Contains($"OCR region preset {expectedPresetName} is not configured yet", exception.Message);
    }

    private sealed class TempCalibrationFile : IDisposable
    {
        private TempCalibrationFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempCalibrationFile Create(OcrRatioRegion ratioRegion)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            string json = $$"""
                {
                  "sourceImageWidth": 1000,
                  "sourceImageHeight": 1000,
                  "regionPixels": { "x": 1, "y": 1, "width": 1, "height": 1 },
                  "regionRatio": { "x": {{ratioRegion.X}}, "y": {{ratioRegion.Y}}, "width": {{ratioRegion.Width}}, "height": {{ratioRegion.Height}} },
                  "generatedAt": "2026-05-15T00:00:00+00:00",
                  "sourceProcessName": "notepad"
                }
                """;
            File.WriteAllText(path, json);
            return new TempCalibrationFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TempTextFile : IDisposable
    {
        private TempTextFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempTextFile Create(string contents)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, contents);
            return new TempTextFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TempImage : IDisposable
    {
        private TempImage(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempImage Create(int width, int height)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            using Bitmap bitmap = new(width, height);
            bitmap.Save(path);
            return new TempImage(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
