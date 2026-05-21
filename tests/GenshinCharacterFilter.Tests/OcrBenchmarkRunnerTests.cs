using System.Drawing;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class OcrBenchmarkRunnerTests
{
    [Fact]
    public async Task RunAsync_RepeatsOcrAndReportsTimings()
    {
        using TempImage image = TempImage.Create();
        FakeOcrService ocr = new();
        OcrBenchmarkRunner runner = new(ocr);

        OcrBenchmarkResult result = await runner.RunAsync(
            new OcrOptions { InputImagePath = image.Path },
            repeat: 3,
            CancellationToken.None);

        Assert.Equal(3, ocr.CallCount);
        Assert.Equal(3, result.Runs.Count);
        Assert.Equal(0, result.FailureCount);
        Assert.Equal(OcrEngine.TesseractCli, result.Engine);
        Assert.True(result.WarmAverageElapsedMs >= 0);
    }

    [Fact]
    public async Task RunAsync_RecordsOcrFailuresWithoutStoppingBenchmark()
    {
        using TempImage image = TempImage.Create();
        FakeOcrService ocr = new() { ThrowOnFirstCall = true };
        OcrBenchmarkRunner runner = new(ocr);

        OcrBenchmarkResult result = await runner.RunAsync(
            new OcrOptions { InputImagePath = image.Path },
            repeat: 2,
            CancellationToken.None);

        Assert.Equal(1, result.FailureCount);
        Assert.NotNull(result.Runs[0].Error);
        Assert.Null(result.Runs[1].Error);
    }

    private sealed class FakeOcrService : IOcrService
    {
        public int CallCount { get; private set; }

        public bool ThrowOnFirstCall { get; init; }

        public Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            if (ThrowOnFirstCall && CallCount == 1)
            {
                throw new OcrException("fake failure");
            }

            return Task.FromResult(new OcrResult("流浪者", options.OcrEngine.ToString(), options.InputImagePath!));
        }
    }

    private sealed class TempImage : IDisposable
    {
        private TempImage(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempImage Create()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
            using Bitmap bitmap = new(4, 4);
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
