using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class PaddleOcrLocalServiceTests
{
    [Fact]
    public void OcrServiceFactory_CreatesTesseractFallback()
    {
        IOcrService service = OcrServiceFactory.Create(OcrEngine.TesseractCli);

        Assert.IsType<TesseractCliOcrService>(service);
    }

    [Fact]
    public void OcrServiceFactory_CreatesPaddleLocalBackend()
    {
        IOcrService service = OcrServiceFactory.Create(OcrEngine.PaddleOcrLocal);

        Assert.IsType<PaddleOcrLocalService>(service);
    }

    [Fact]
    public void ValidateRuntimeOptions_RejectsMissingRuntimeDirectory()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = CreatePaddleOptions(input.Path);
        options.PaddleRuntimeDirectory = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");

        OcrException exception = Assert.Throws<OcrException>(
            () => PaddleOcrLocalService.ValidateRuntimeOptions(options));

        Assert.Contains("runtime directory", exception.Message);
    }

    [Fact]
    public void ValidateRuntimeOptions_RejectsMissingModelDirectory()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = CreatePaddleOptions(input.Path);
        options.PaddleModelDirectory = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");

        OcrException exception = Assert.Throws<OcrException>(
            () => PaddleOcrLocalService.ValidateRuntimeOptions(options));

        Assert.Contains("model directory", exception.Message);
    }

    [Fact]
    public void Validate_PaddleDoesNotRequireTesseractExecutablePath()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = CreatePaddleOptions(input.Path);
        options.TesseractExecutablePath = " ";

        options.Validate();
    }

    private static OcrOptions CreatePaddleOptions(string inputPath)
    {
        return new OcrOptions
        {
            OcrEngine = OcrEngine.PaddleOcrLocal,
            InputImagePath = inputPath
        };
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
