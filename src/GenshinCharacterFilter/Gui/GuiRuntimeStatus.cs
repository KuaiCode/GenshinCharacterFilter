using GenshinCharacterFilter.Detection;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Tracks the compact status shown in the persistent WPF control dock.
/// </summary>
public sealed class GuiRuntimeStatus
{
    public GuiStatusSnapshot Snapshot { get; private set; } = new(
        GuiRuntimeRunState.Idle,
        GuiAudioState.Restored,
        RequestedCaptureBackend: "VisiblePixels",
        ActualCaptureBackend: "VisiblePixels",
        CaptureStatus: "Ready",
        LastOcrText: "(none)",
        LastDetectedSpeaker: "(none)",
        LastAudioAction: "none");

    public GuiStatusSnapshot MarkStarting() => Update(Snapshot with { RunState = GuiRuntimeRunState.Starting });

    public GuiStatusSnapshot MarkDetecting() => Update(Snapshot with { RunState = GuiRuntimeRunState.Detecting });

    public GuiStatusSnapshot MarkStopping() => Update(Snapshot with { RunState = GuiRuntimeRunState.Stopping });

    public GuiStatusSnapshot MarkIdle() => Update(Snapshot with
    {
        RunState = GuiRuntimeRunState.Idle,
        AudioState = GuiAudioState.Restored,
        LastAudioAction = Snapshot.AudioState is GuiAudioState.Reduced or GuiAudioState.Muted
            ? "restore"
            : Snapshot.LastAudioAction
    });

    public GuiStatusSnapshot MarkError() => Update(Snapshot with { RunState = GuiRuntimeRunState.Error });

    public GuiStatusSnapshot MarkCaptureLost() => Update(Snapshot with
    {
        RunState = GuiRuntimeRunState.CaptureLost,
        AudioState = GuiAudioState.Restored,
        LastAudioAction = Snapshot.AudioState is GuiAudioState.Reduced or GuiAudioState.Muted
            ? "restore"
            : "none (already restored)"
    });

    public GuiStatusSnapshot MarkReconnecting() => Update(Snapshot with { RunState = GuiRuntimeRunState.Reconnecting });

    public GuiStatusSnapshot SetCaptureBackend(string backend, string status)
    {
        string normalized = NormalizeBackend(backend);
        return SetCaptureBackend(normalized, normalized, status);
    }

    public GuiStatusSnapshot SetCaptureBackend(string requestedBackend, string actualBackend, string status) => Update(Snapshot with
    {
        RequestedCaptureBackend = NormalizeBackend(requestedBackend),
        ActualCaptureBackend = NormalizeBackend(actualBackend),
        CaptureStatus = string.IsNullOrWhiteSpace(status) ? "(unknown)" : status.Trim()
    });

    public GuiStatusSnapshot ApplyObservation(GuiLastObservation observation)
    {
        GuiAudioState audioState = observation.AudioAction switch
        {
            DetectionAudioAction.Mute => GuiAudioState.Muted,
            DetectionAudioAction.Reduce => GuiAudioState.Reduced,
            DetectionAudioAction.Restore => GuiAudioState.Restored,
            _ => Snapshot.AudioState
        };

        GuiRuntimeRunState runState = observation.AudioAction switch
        {
            DetectionAudioAction.Mute or DetectionAudioAction.Reduce => GuiRuntimeRunState.Reduced,
            DetectionAudioAction.Restore => GuiRuntimeRunState.Restored,
            _ => GuiRuntimeRunState.Detecting
        };

        string speaker = !string.IsNullOrWhiteSpace(observation.StableSpeaker)
            ? observation.StableSpeaker!
            : !string.IsNullOrWhiteSpace(observation.RawSpeaker)
                ? observation.RawSpeaker!
                : "(none)";

        return Update(Snapshot with
        {
            RunState = runState,
            AudioState = audioState,
            LastOcrText = FormatValue(observation.OcrText),
            LastDetectedSpeaker = speaker,
            LastAudioAction = FormatAudioAction(observation.AudioAction)
        });
    }

    public static GuiLastObservation FromDetectionResult(DetectionDryRunResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new GuiLastObservation(
            result.OcrResult.RawText,
            result.StabilityResult.StableState.MatchedSpeaker,
            result.SpeakerMatchResult.MatchedSpeaker,
            result.DetectionAudioActionResult?.Action ?? DetectionAudioAction.None);
    }

    private GuiStatusSnapshot Update(GuiStatusSnapshot snapshot)
    {
        Snapshot = snapshot;
        return Snapshot;
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Trim();
    }

    private static string NormalizeBackend(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "(unknown)" : value.Trim();
    }

    private static string FormatAudioAction(DetectionAudioAction action)
    {
        return action switch
        {
            DetectionAudioAction.Mute => "mute",
            DetectionAudioAction.Reduce => "reduce",
            DetectionAudioAction.Restore => "restore",
            _ => "none"
        };
    }
}
