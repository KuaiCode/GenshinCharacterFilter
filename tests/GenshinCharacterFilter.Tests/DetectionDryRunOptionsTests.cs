using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionDryRunOptionsTests
{
    [Fact]
    public void Defaults_AreSafeForExplicitDryRun()
    {
        DetectionDryRunOptions options = new();

        Assert.Equal(DetectionDryRunOptions.DefaultLoopIntervalMs, options.LoopIntervalMs);
        Assert.Null(options.LoopCount);
        Assert.Null(options.OcrInputPath);
        Assert.Null(options.TargetProcessName);
        Assert.Empty(options.TargetSpeakers);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(10000)]
    public void ValidateLoopIntervalMs_AcceptsAllowedRange(int intervalMs)
    {
        DetectionDryRunOptions.ValidateLoopIntervalMs(intervalMs);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(10001)]
    public void ValidateLoopIntervalMs_RejectsOutOfRangeValues(int intervalMs)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DetectionDryRunOptions.ValidateLoopIntervalMs(intervalMs));
    }

    [Fact]
    public void ValidateLoopCount_AllowsNullForRunUntilCancellation()
    {
        DetectionDryRunOptions.ValidateLoopCount(null);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void ValidateLoopCount_AcceptsPositiveValues(int loopCount)
    {
        DetectionDryRunOptions.ValidateLoopCount(loopCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateLoopCount_RejectsNonPositiveValues(int loopCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DetectionDryRunOptions.ValidateLoopCount(loopCount));
    }

    [Fact]
    public void Validate_RequiresInputImageOrProcess()
    {
        DetectionDryRunOptions options = new();

        ArgumentException exception = Assert.Throws<ArgumentException>(options.Validate);

        Assert.Contains("--ocr-input", exception.Message);
        Assert.Contains("--process", exception.Message);
    }

    [Fact]
    public void Validate_ProcessModeAllowsFullImageDryRun()
    {
        DetectionDryRunOptions options = new()
        {
            TargetProcessName = "notepad"
        };

        options.Validate();
    }

    [Fact]
    public void Validate_FixedImageModeDoesNotRequireRegion()
    {
        DetectionDryRunOptions options = new()
        {
            OcrInputPath = "input.png"
        };

        options.Validate();
    }

    [Fact]
    public void Validate_ProcessModeAcceptsRegion()
    {
        DetectionDryRunOptions options = new()
        {
            TargetProcessName = "notepad",
            OcrRegion = new OcrRegion(1, 2, 3, 4)
        };

        options.Validate();
    }

    [Fact]
    public void Validate_RejectsAmbiguousOcrRegionSources()
    {
        DetectionDryRunOptions options = new()
        {
            OcrInputPath = "input.png",
            OcrRegion = new OcrRegion(1, 2, 3, 4),
            OcrRegionConfigPath = "ocr-region.json"
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }
}
