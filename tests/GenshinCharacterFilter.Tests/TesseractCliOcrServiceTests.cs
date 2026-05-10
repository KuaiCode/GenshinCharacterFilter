using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class TesseractCliOcrServiceTests
{
    [Fact]
    public void BuildArguments_UsesStdoutLanguageAndPageSegmentationMode()
    {
        using TempFile input = TempFile.Create();
        OcrOptions options = new()
        {
            InputImagePath = input.Path,
            Language = "chi_sim+eng",
            PageSegmentationMode = 7
        };

        IReadOnlyList<string> arguments = TesseractCliOcrService.BuildArguments(options);

        Assert.Equal(Path.GetFullPath(input.Path), arguments[0]);
        Assert.Equal("stdout", arguments[1]);
        Assert.Equal("-l", arguments[2]);
        Assert.Equal("chi_sim+eng", arguments[3]);
        Assert.Equal("--psm", arguments[4]);
        Assert.Equal("7", arguments[5]);
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
