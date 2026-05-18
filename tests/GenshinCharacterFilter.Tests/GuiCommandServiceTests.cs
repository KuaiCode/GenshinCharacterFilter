using GenshinCharacterFilter;
using GenshinCharacterFilter.Gui;

namespace GenshinCharacterFilter.Tests;

public sealed class GuiCommandServiceTests
{
    [Fact]
    public void GetDefaultOcrInputPath_UsesOriginalCapturePath()
    {
        string path = GuiCommandService.GetDefaultOcrInputPath();

        Assert.Equal(Path.Combine("debug-captures", "capture-latest.png"), path);
        Assert.DoesNotContain("debug-ocr", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_UncheckedUsesLiveCapture()
    {
        string? path = GuiCommandService.ResolveDetectionLoopOcrInputPath(
            Path.Combine("debug-captures", "capture-latest.png"),
            useFixedImageForDetection: false);

        Assert.Null(path);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_CheckedUsesFixedImage()
    {
        string inputPath = Path.Combine("debug-captures", "capture-latest.png");

        string? path = GuiCommandService.ResolveDetectionLoopOcrInputPath(
            $"  {inputPath}  ",
            useFixedImageForDetection: true);

        Assert.Equal(inputPath, path);
    }

    [Fact]
    public void ResolveDetectionLoopOcrInputPath_CheckedRequiresImagePath()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiCommandService.ResolveDetectionLoopOcrInputPath(
                " ",
                useFixedImageForDetection: true));

        Assert.Contains("fixed-image detection", exception.Message);
    }

    [Fact]
    public void EnsureGuardedRealAudioAllowsDetectionInput_RejectsFixedImageMode()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => GuiCommandService.EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection: true));

        Assert.Contains("does not allow fixed-image", exception.Message);
    }

    [Fact]
    public void EnsureGuardedRealAudioAllowsDetectionInput_AllowsLiveCaptureMode()
    {
        GuiCommandService.EnsureGuardedRealAudioAllowsDetectionInput(useFixedImageForDetection: false);
    }

    [Fact]
    public void ParseGuiDetectionTuning_RunUntilStopUsesNullLoopCount()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: " ",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.True(options.RunUntilStop);
        Assert.Null(options.LoopCount);
        Assert.Equal("until stopped", options.FormatLoopCount(5));
    }

    [Fact]
    public void ParseGuiDetectionTuning_FixedLoopCountUsesNumber()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: false,
            loopCount: " 20 ",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.False(options.RunUntilStop);
        Assert.Equal(20, options.LoopCount);
        Assert.Equal("20", options.FormatLoopCount(20));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void ParseGuiDetectionTuning_RejectsInvalidLoopCount(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiDetectionTuningOptions.Parse(
                runUntilStop: false,
                loopCount: value,
                loopIntervalMs: "200",
                captureDelayMs: "100",
                matchThreshold: "2",
                missThreshold: "1"));

        Assert.Contains("Loop count", exception.Message);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsRealtimeDefaults()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "200",
            captureDelayMs: "100",
            matchThreshold: "2",
            missThreshold: "1");

        Assert.Equal(200, options.LoopIntervalMs);
        Assert.Equal(100, options.CaptureDelayMs);
        Assert.Equal(2, options.MatchThreshold);
        Assert.Equal(1, options.MissThreshold);
        Assert.False(options.SaveDebugImages);
    }

    [Fact]
    public void ParseGuiDetectionTuning_ReadsSaveDebugImages()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "",
            saveDebugImages: true);

        Assert.True(options.SaveDebugImages);
    }

    [Theory]
    [InlineData("abc", "100", "2", "1", "Loop interval")]
    [InlineData("49", "100", "2", "1", "Loop interval")]
    [InlineData("200", "abc", "2", "1", "Capture delay")]
    [InlineData("200", "5001", "2", "1", "Capture delay")]
    [InlineData("200", "100", "0", "1", "Match threshold")]
    [InlineData("200", "100", "2", "0", "Miss threshold")]
    public void ParseGuiDetectionTuning_RejectsInvalidTimingAndThresholds(
        string loopIntervalMs,
        string captureDelayMs,
        string matchThreshold,
        string missThreshold,
        string expectedMessage)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => GuiDetectionTuningOptions.Parse(
                runUntilStop: true,
                loopCount: "",
                loopIntervalMs: loopIntervalMs,
                captureDelayMs: captureDelayMs,
                matchThreshold: matchThreshold,
                missThreshold: missThreshold));

        Assert.Contains(expectedMessage, exception.Message);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_RunUntilStopOverridesConfiguredLoopCount()
    {
        AppSettings settings = new();
        settings.Detection.LoopCount = 5;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "");

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Null(settings.Detection.LoopCount);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_NumberOverridesConfiguredLoopCount()
    {
        AppSettings settings = new();
        settings.Detection.LoopCount = 5;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: false,
            loopCount: "10",
            loopIntervalMs: "",
            captureDelayMs: "",
            matchThreshold: "",
            missThreshold: "");

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Equal(10, settings.Detection.LoopCount);
    }

    [Fact]
    public void ApplyGuiDetectionTuningOverrides_OverridesIntervalAndThresholds()
    {
        AppSettings settings = new();
        settings.Detection.LoopIntervalMs = 1000;
        settings.Detection.MatchThreshold = 3;
        settings.Detection.MissThreshold = 3;
        settings.Detection.SaveDebugImages = true;
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "200",
            captureDelayMs: "",
            matchThreshold: "2",
            missThreshold: "1",
            saveDebugImages: false);

        GuiCommandService.ApplyGuiDetectionTuningOverrides(settings, options);

        Assert.Equal(200, settings.Detection.LoopIntervalMs);
        Assert.Equal(2, settings.Detection.MatchThreshold);
        Assert.Equal(1, settings.Detection.MissThreshold);
        Assert.False(settings.Detection.SaveDebugImages);
    }

    [Fact]
    public void ResolveGuiCaptureDelayMs_UsesGuiOverrideWhenSpecified()
    {
        GuiDetectionTuningOptions options = GuiDetectionTuningOptions.Parse(
            runUntilStop: true,
            loopCount: "",
            loopIntervalMs: "",
            captureDelayMs: "100",
            matchThreshold: "",
            missThreshold: "");

        int captureDelayMs = GuiCommandService.ResolveGuiCaptureDelayMs(500, options);

        Assert.Equal(100, captureDelayMs);
    }
}
