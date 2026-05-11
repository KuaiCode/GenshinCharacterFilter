using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionDryRunLoopTests
{
    [Fact]
    public async Task RunAsync_LoopCountTwoCallsFakeOcrTwice()
    {
        using TempFile input = TempFile.Create();
        FakeOcrService ocr = new(["target", "target"]);
        FakeSpeakerMatcher matcher = new();
        FakeWindowCapture capture = new();
        StringWriter log = new();
        DetectionDryRunLoop loop = new(ocr, matcher, capture, log: log);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        Assert.Equal(2, ocr.CallCount);
        Assert.Equal(2, matcher.CallCount);
    }

    [Fact]
    public async Task RunAsync_FixedImageModeDoesNotCallCapture()
    {
        using TempFile input = TempFile.Create();
        FakeWindowCapture capture = new();
        DetectionDryRunLoop loop = new(new FakeOcrService(["target", "target"]), new FakeSpeakerMatcher(), capture);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        Assert.Equal(0, capture.CallCount);
    }

    [Fact]
    public async Task RunAsync_ReportsStableStateChangeWhenThresholdIsReached()
    {
        using TempFile input = TempFile.Create();
        StringWriter log = new();
        DetectionDryRunLoop loop = new(
            new FakeOcrService(["target", "target"]),
            new FakeSpeakerMatcher(),
            new FakeWindowCapture(),
            log: log);

        await loop.RunAsync(CreateOptions(input.Path), CancellationToken.None);

        string output = log.ToString();
        Assert.Contains("Raw matched: True", output);
        Assert.Contains("Stable matched: True", output);
        Assert.Contains("Consecutive match count: 2", output);
        Assert.Contains("Stable state changed: NotMatched -> Matched(Wanderer)", output);
    }

    private static DetectionDryRunOptions CreateOptions(string inputPath)
    {
        return new DetectionDryRunOptions
        {
            OcrInputPath = inputPath,
            LoopCount = 2,
            LoopIntervalMs = DetectionDryRunOptions.MinLoopIntervalMs,
            TargetSpeakers = ["Wanderer"],
            Stability = new DetectionStabilityOptions
            {
                MatchThreshold = 2,
                MissThreshold = 2
            }
        };
    }

    private sealed class FakeOcrService : IOcrService
    {
        private readonly Queue<string> _rawTexts;

        public FakeOcrService(IEnumerable<string> rawTexts)
        {
            _rawTexts = new Queue<string>(rawTexts);
        }

        public int CallCount { get; private set; }

        public Task<OcrResult> ExtractTextAsync(OcrOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            string rawText = _rawTexts.Count > 0 ? _rawTexts.Dequeue() : string.Empty;
            return Task.FromResult(new OcrResult(rawText, "FakeOcr", options.InputImagePath!));
        }
    }

    private sealed class FakeSpeakerMatcher : ISpeakerMatcher
    {
        public int CallCount { get; private set; }

        public SpeakerMatchResult Match(string? rawText, SpeakerMatcherOptions options)
        {
            CallCount++;
            bool matched = string.Equals(rawText, "target", StringComparison.OrdinalIgnoreCase);
            return matched
                ? new SpeakerMatchResult(true, "Wanderer", rawText ?? string.Empty, rawText ?? string.Empty)
                : new SpeakerMatchResult(false, null, rawText ?? string.Empty, rawText ?? string.Empty);
        }
    }

    private sealed class FakeWindowCapture : IGameWindowCapture
    {
        public int CallCount { get; private set; }

        public Task<string> CaptureOnceAsync(WindowCaptureOptions options, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult("capture.png");
        }
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
            File.WriteAllText(path, "fake image placeholder");
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
