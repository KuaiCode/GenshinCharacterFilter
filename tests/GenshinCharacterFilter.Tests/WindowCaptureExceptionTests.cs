using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Tests;

public sealed class WindowCaptureExceptionTests
{
    [Fact]
    public void TargetWindowMinimizedCannotRestore_IncludesReasonAndUserGuidance()
    {
        WindowCaptureException exception = WindowCaptureException.TargetWindowMinimizedCannotRestore("YuanShen");

        Assert.Equal(WindowCaptureFailureReason.TargetWindowMinimizedCannotRestore, exception.Reason);
        Assert.Contains("YuanShen", exception.Message);
        Assert.Contains("visible screen pixels", exception.Message);
        Assert.Contains("borderless/windowed", exception.Message);
        Assert.Contains("Exclusive fullscreen", exception.Message);
    }

    [Fact]
    public void DefaultConstructor_UsesUnknownReason()
    {
        WindowCaptureException exception = new("capture failed");

        Assert.Equal(WindowCaptureFailureReason.Unknown, exception.Reason);
    }

    [Fact]
    public void ForegroundProcessValidator_AcceptsMatchingExeAndPathNames()
    {
        WindowCaptureProcessValidator.ValidateForegroundProcess("YuanShen", @"C:\Games\YuanShen.exe");
    }

    [Fact]
    public void ForegroundProcessValidator_RejectsWrongForegroundProcess()
    {
        WindowCaptureException exception = Assert.Throws<WindowCaptureException>(
            () => WindowCaptureProcessValidator.ValidateForegroundProcess("YuanShen", "notepad"));

        Assert.Equal(WindowCaptureFailureReason.ForegroundWindowProcessMismatch, exception.Reason);
        Assert.Contains("Current foreground window is not 'YuanShen'", exception.Message);
        Assert.Contains("notepad", exception.Message);
    }
}
