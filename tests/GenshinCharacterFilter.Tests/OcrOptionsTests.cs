using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrOptionsTests
{
    [Fact]
    public void Defaults_UseTesseractCliForChineseAndEnglishText()
    {
        OcrOptions options = new();

        Assert.Equal(OcrEngine.TesseractCli, options.OcrEngine);
        Assert.Equal("tesseract", options.TesseractExecutablePath);
        Assert.Equal("chi_sim+eng", options.Language);
        Assert.Equal(7, options.PageSegmentationMode);
        Assert.Null(options.InputImagePath);
        Assert.Null(options.OcrRegion);
    }

    [Fact]
    public void Validate_AcceptsExistingInputImage()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = new()
        {
            InputImagePath = input.Path
        };

        options.Validate();
    }

    [Fact]
    public void Validate_RejectsMissingInputImage()
    {
        OcrOptions options = new()
        {
            InputImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png")
        };

        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(options.Validate);
        Assert.Contains("OCR input image was not found", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsBlankLanguage(string language)
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            Language = language
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(14)]
    public void Validate_RejectsPageSegmentationModeOutsideTesseractRange(int pageSegmentationMode)
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            PageSegmentationMode = pageSegmentationMode
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Fact]
    public void Validate_RejectsInvalidRegionShape()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            OcrRegion = new OcrRegion(0, 0, 0, 10)
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    private sealed class TempFile : IDisposable
    {
        private TempFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempFile Create()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            File.WriteAllBytes(path, [0]);
            return new TempFile(path);
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
