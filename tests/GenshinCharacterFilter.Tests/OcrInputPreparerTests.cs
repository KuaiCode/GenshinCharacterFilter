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
        using Image croppedImage = Image.FromFile(preparedPath);
        Assert.Equal(5, croppedImage.Width);
        Assert.Equal(4, croppedImage.Height);
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
