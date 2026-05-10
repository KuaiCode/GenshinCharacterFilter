using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Coordination;

/// <summary>
/// Coordinates one speaker detection result with the current audio mute state.
/// </summary>
public sealed class MuteCoordinator
{
    private readonly ISpeakerDetector _speakerDetector;
    private readonly IAudioMuteService _audioMuteService;
    private readonly MuteCoordinatorOptions _options;

    public MuteCoordinator(
        ISpeakerDetector speakerDetector,
        IAudioMuteService audioMuteService,
        MuteCoordinatorOptions options)
    {
        _speakerDetector = speakerDetector ?? throw new ArgumentNullException(nameof(speakerDetector));
        _audioMuteService = audioMuteService ?? throw new ArgumentNullException(nameof(audioMuteService));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.TargetSpeakers.Count == 0)
        {
            throw new ArgumentException("At least one target speaker is required.", nameof(options));
        }
    }

    /// <summary>
    /// Gets the current coordination state.
    /// </summary>
    public MuteCoordinatorState State { get; private set; } = MuteCoordinatorState.Idle;

    /// <summary>
    /// Runs one detection and audio coordination step.
    /// </summary>
    public async Task TickAsync(CancellationToken cancellationToken)
    {
        string? speaker;

        try
        {
            speaker = await _speakerDetector.DetectSpeakerAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // 检测失败时优先恢复，避免上一帧的静音状态残留。
            await RestoreIfMutedAsync(cancellationToken);
            State = MuteCoordinatorState.Faulted;
            return;
        }

        if (IsTargetSpeaker(speaker))
        {
            await MuteIfNeededAsync(cancellationToken);
            return;
        }

        await RestoreIfMutedAsync(cancellationToken);

        if (State == MuteCoordinatorState.Faulted || State == MuteCoordinatorState.Restoring)
        {
            State = MuteCoordinatorState.Idle;
        }
    }

    private async Task MuteIfNeededAsync(CancellationToken cancellationToken)
    {
        if (State == MuteCoordinatorState.Muted)
        {
            return;
        }

        await _audioMuteService.MuteAsync(cancellationToken);
        State = MuteCoordinatorState.Muted;
    }

    private async Task RestoreIfMutedAsync(CancellationToken cancellationToken)
    {
        if (State != MuteCoordinatorState.Muted)
        {
            return;
        }

        State = MuteCoordinatorState.Restoring;
        await _audioMuteService.RestoreAsync(cancellationToken);
        State = MuteCoordinatorState.Idle;
    }

    private bool IsTargetSpeaker(string? speaker)
    {
        if (string.IsNullOrWhiteSpace(speaker))
        {
            return false;
        }

        StringComparison comparison = _options.CaseSensitiveSpeakerMatching
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return _options.TargetSpeakers.Any(targetSpeaker =>
            string.Equals(targetSpeaker, speaker.Trim(), comparison));
    }
}
