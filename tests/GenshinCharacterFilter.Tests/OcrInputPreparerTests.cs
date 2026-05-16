using System.Drawing;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrInputPreparerTests
{
    [Fact]
    public void PrepareInput_WithoutRegionReturnsOriginalPath()
    {
        using TempImage input = TempImage.Create(10, 10);
        OcrOptions options = new()
        {
            InputImagePath = input.Path
        };

        string preparedPath = new OcrInputPreparer().PrepareInput(options);

        Assert.Equal(Path.GetFullPath(input.Path), preparedPath);
    }

    [Fact]
    public void PrepareInput_WithRegionWritesCroppedDebugImage()
    {
        using TempImage input = TempImage.Create(20, 10);
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            OcrRegion = new OcrRegion(2, 3, 5, 4)
        };

        string preparedPath = new OcrInputPreparer().PrepareInput(options);

        Assert.EndsWith(Path.Combine("debug-ocr", "ocr-input-latest.png"), preparedPath);
        Assert.True(File.Exists(preparedPath));
        using Image croppedImage = Image.FromFile(preparedPath);
        Assert.Equal(5, croppedImage.Width);
        Assert.Equal(4, croppedImage.Height);
    }

    [Fact]
    public void PrepareInput_WithDebugOutputAsInputThrowsClearError()
    {
        string debugDirectory = Path.GetFullPath(OcrInputPreparer.DefaultDebugOutputDirectory);
        Directory.CreateDirectory(debugDirectory);
        string debugOutputPath = Path.Combine(debugDirectory, OcrInputPreparer.DefaultDebugInputFileName);
        using (Bitmap bitmap = new(20, 10))
        {
            bitmap.Save(debugOutputPath);
        }

        OcrOptions options = new()
        {
            InputImagePath = debugOutputPath,
            OcrRegion = new OcrRegion(0, 0, 5, 4)
        };

        OcrException exception = Assert.Throws<OcrException>(() => new OcrInputPreparer().PrepareInput(options));

        Assert.Contains("debug OCR output", exception.Message);
        Assert.Contains("debug-captures", exception.Message);
    }

    [Fact]
    public void PrepareInput_WithRegionCanReadSourceWhileImageHandleIsOpen()
    {
        using TempImage input = TempImage.Create(20, 10);
        using Image _ = Image.FromFile(input.Path);
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            OcrRegion = new OcrRegion(1, 1, 6, 3)
        };

        string preparedPath = new OcrInputPreparer().PrepareInput(options);

        using Image croppedImage = Image.FromFile(preparedPath);
        Assert.Equal(6, croppedImage.Width);
        Assert.Equal(3, croppedImage.Height);
    }

    [Fact]
    public void PrepareInput_WithOutOfBoundsRegionThrowsClearError()
    {
        using TempImage input = TempImage.Create(20, 10);
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            OcrRegion = new OcrRegion(18, 0, 5, 4)
        };

        OcrException exception = Assert.Throws<OcrException>(() => new OcrInputPreparer().PrepareInput(options));
        Assert.Contains("does not fit within input image", exception.Message);
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
            if (width >= 2 && height >= 1)
            {
                bitmap.SetPixel(0, 0, Color.Black);
                bitmap.SetPixel(1, 0, Color.White);
            }

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
