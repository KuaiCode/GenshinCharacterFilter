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
    public void ForegroundCaptureLost_IncludesReasonAndRecoveryGuidance()
    {
        WindowCaptureException exception = WindowCaptureException.ForegroundCaptureLost("YuanShen", "minimized");

        Assert.Equal(WindowCaptureFailureReason.ForegroundCaptureLost, exception.Reason);
        Assert.True(WindowCaptureException.IsForegroundCaptureLost(exception));
        Assert.Contains("YuanShen", exception.Message);
        Assert.Contains("Detection stopped safely", exception.Message);
        Assert.Contains("Restore audio was attempted", exception.Message);
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

    [Fact]
    public void TargetWindowActivationResult_SuccessUsesNoFailureReason()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Succeeded(TargetWindowActivationMethod.Win32);

        Assert.True(result.Success);
        Assert.Equal(TargetWindowActivationFailureReason.None, result.FailureReason);
        Assert.Equal(TargetWindowActivationMethod.Win32, result.Method);
        Assert.False(result.InputFallbackAttempted);
    }

    [Fact]
    public void TargetWindowActivationResult_InputFallbackSuccessMarksFallbackAttempted()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Succeeded(TargetWindowActivationMethod.InputFallback);

        Assert.True(result.Success);
        Assert.True(result.InputFallbackAttempted);
        Assert.Contains("InputFallback", result.UserMessage);
    }

    [Fact]
    public void TargetWindowActivationResult_FailureKeepsUserMessage()
    {
        TargetWindowActivationResult result = TargetWindowActivationResult.Failed(
            TargetWindowActivationFailureReason.ForegroundMismatch,
            "Foreground window is not target.",
            inputFallbackAttempted: true);

        Assert.False(result.Success);
        Assert.Equal(TargetWindowActivationFailureReason.ForegroundMismatch, result.FailureReason);
        Assert.Equal("Foreground window is not target.", result.UserMessage);
        Assert.True(result.InputFallbackAttempted);
    }
}
