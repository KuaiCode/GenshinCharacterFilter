using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Gui;
using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class GuiRuntimeStatusTests
{
    [Fact]
    public void StartsIdleWithRestoredAudioState()
    {
        GuiRuntimeStatus status = new();

        GuiStatusSnapshot snapshot = status.Snapshot;

        Assert.Equal(GuiRuntimeRunState.Idle, snapshot.RunState);
        Assert.Equal(GuiAudioState.Restored, snapshot.AudioState);
        Assert.Equal("VisiblePixels", snapshot.CaptureBackend);
        Assert.Equal("Ready", snapshot.CaptureStatus);
        Assert.Equal("(none)", snapshot.LastOcrText);
        Assert.Equal("(none)", snapshot.LastDetectedSpeaker);
        Assert.Equal("none", snapshot.LastAudioAction);
    }

    [Fact]
    public void SupportsRequestedRunStateTransitions()
    {
        GuiRuntimeStatus status = new();

        Assert.Equal(GuiRuntimeRunState.Starting, status.MarkStarting().RunState);
        Assert.Equal(GuiRuntimeRunState.Detecting, status.MarkDetecting().RunState);
        Assert.Equal(GuiRuntimeRunState.CaptureLost, status.MarkCaptureLost().RunState);
        Assert.Equal(GuiRuntimeRunState.Reconnecting, status.MarkReconnecting().RunState);
        Assert.Equal(GuiRuntimeRunState.Stopping, status.MarkStopping().RunState);
        Assert.Equal(GuiRuntimeRunState.Error, status.MarkError().RunState);
        Assert.Equal(GuiRuntimeRunState.Idle, status.MarkIdle().RunState);
    }

    [Fact]
    public void ApplyObservation_StoresLastOcrSpeakerAndReduceAction()
    {
        GuiRuntimeStatus status = new();
        DetectionDryRunResult result = CreateResult(
            rawText: "流浪者",
            stableSpeaker: "流浪者",
            rawSpeaker: "流浪者",
            DetectionAudioAction.Reduce);

        GuiStatusSnapshot snapshot = status.ApplyObservation(GuiRuntimeStatus.FromDetectionResult(result));

        Assert.Equal(GuiRuntimeRunState.Reduced, snapshot.RunState);
        Assert.Equal(GuiAudioState.Reduced, snapshot.AudioState);
        Assert.Equal("流浪者", snapshot.LastOcrText);
        Assert.Equal("流浪者", snapshot.LastDetectedSpeaker);
        Assert.Equal("reduce", snapshot.LastAudioAction);
    }

    [Fact]
    public void ApplyObservation_RestoreUpdatesAudioState()
    {
        GuiRuntimeStatus status = new();

        GuiStatusSnapshot snapshot = status.ApplyObservation(new GuiLastObservation(
            OcrText: string.Empty,
            StableSpeaker: null,
            RawSpeaker: null,
            DetectionAudioAction.Restore));

        Assert.Equal(GuiRuntimeRunState.Restored, snapshot.RunState);
        Assert.Equal(GuiAudioState.Restored, snapshot.AudioState);
        Assert.Equal("(empty)", snapshot.LastOcrText);
        Assert.Equal("(none)", snapshot.LastDetectedSpeaker);
        Assert.Equal("restore", snapshot.LastAudioAction);
    }

    [Fact]
    public void MarkReconnecting_AfterCaptureLostShowsReconnectState()
    {
        GuiRuntimeStatus status = new();
        status.MarkCaptureLost();

        GuiStatusSnapshot snapshot = status.MarkReconnecting();

        Assert.Equal(GuiRuntimeRunState.Reconnecting, snapshot.RunState);
    }

    [Fact]
    public void MarkCaptureLost_AfterReducedShowsRestoredAudioState()
    {
        GuiRuntimeStatus status = new();
        status.ApplyObservation(new GuiLastObservation(
            OcrText: "target",
            StableSpeaker: "target",
            RawSpeaker: "target",
            DetectionAudioAction.Reduce));

        GuiStatusSnapshot snapshot = status.MarkCaptureLost();

        Assert.Equal(GuiRuntimeRunState.CaptureLost, snapshot.RunState);
        Assert.Equal(GuiAudioState.Restored, snapshot.AudioState);
        Assert.Equal("restore", snapshot.LastAudioAction);
    }

    [Fact]
    public void MarkCaptureLost_WhenAlreadyRestoredShowsNotFilteringState()
    {
        GuiRuntimeStatus status = new();

        GuiStatusSnapshot snapshot = status.MarkCaptureLost();

        Assert.Equal(GuiRuntimeRunState.CaptureLost, snapshot.RunState);
        Assert.Equal(GuiAudioState.Restored, snapshot.AudioState);
        Assert.Equal("none (already restored)", snapshot.LastAudioAction);
    }

    [Fact]
    public void SetCaptureBackend_UpdatesBackendStatus()
    {
        GuiRuntimeStatus status = new();

        GuiStatusSnapshot snapshot = status.SetCaptureBackend("WindowsGraphicsCapture", "Unavailable");

        Assert.Equal("WindowsGraphicsCapture", snapshot.CaptureBackend);
        Assert.Equal("Unavailable", snapshot.CaptureStatus);
    }

    private static DetectionDryRunResult CreateResult(
        string rawText,
        string? stableSpeaker,
        string? rawSpeaker,
        DetectionAudioAction audioAction)
    {
        DetectionStableState stableState = stableSpeaker is null
            ? DetectionStableState.NotMatched
            : new DetectionStableState(true, stableSpeaker);

        return new DetectionDryRunResult(
            Iteration: 1,
            OcrResult: new OcrResult(rawText, "FakeOcr", "input.png"),
            SpeakerMatchResult: new SpeakerMatchResult(rawSpeaker is not null, rawSpeaker, rawText, rawText),
            StabilityResult: new DetectionStabilityResult(
                RawMatched: rawSpeaker is not null,
                RawMatchedSpeaker: rawSpeaker,
                StableState: stableState,
                PreviousStableState: DetectionStableState.NotMatched,
                StableStateChanged: stableSpeaker is not null,
                ConsecutiveMatchCount: stableSpeaker is null ? 0 : 1,
                ConsecutiveMissCount: stableSpeaker is null ? 1 : 0),
            DetectionAudioActionResult: new DetectionAudioActionResult(audioAction, audioAction is not DetectionAudioAction.None));
    }
}
