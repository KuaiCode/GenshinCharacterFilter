using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class WindowCaptureOptionsTests
{
    [Fact]
    public void Defaults_AreSafeForDebugCapture()
    {
        WindowCaptureOptions options = new();

        Assert.Equal("GenshinImpact", options.TargetProcessName);
        Assert.Null(options.CaptureRegion);
        Assert.Equal("debug-captures", options.OutputDirectory);
        Assert.Equal("capture-latest.png", options.OutputFileName);
        Assert.Equal(500, options.CaptureDelayMs);
    }

    [Theory]
    [InlineData("chrome", "chrome")]
    [InlineData("chrome.exe", "chrome")]
    [InlineData("  chrome.exe  ", "chrome")]
    [InlineData(@"C:\Tools\notepad.exe", "notepad")]
    public void NormalizeProcessName_RemovesExeSuffixAndPath(string input, string expected)
    {
        string normalized = WindowCaptureOptions.NormalizeProcessName(input);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void NormalizeProcessName_RejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => WindowCaptureOptions.NormalizeProcessName(" "));
    }

    [Fact]
    public void GetOutputPath_CombinesDirectoryAndFileName()
    {
        WindowCaptureOptions options = new()
        {
            OutputDirectory = "debug-captures",
            OutputFileName = "capture-latest.png"
        };

        string outputPath = options.GetOutputPath();

        Assert.EndsWith(Path.Combine("debug-captures", "capture-latest.png"), outputPath);
    }

    [Fact]
    public void Validate_RejectsNonPngFileName()
    {
        WindowCaptureOptions options = new()
        {
            OutputFileName = "capture-latest.bmp"
        };

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(5000)]
    public void Validate_AcceptsCaptureDelayInRange(int captureDelayMs)
    {
        WindowCaptureOptions options = new()
        {
            CaptureDelayMs = captureDelayMs
        };

        options.Validate();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5001)]
    public void Validate_RejectsCaptureDelayOutsideRange(int captureDelayMs)
    {
        WindowCaptureOptions options = new()
        {
            CaptureDelayMs = captureDelayMs
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }
}
