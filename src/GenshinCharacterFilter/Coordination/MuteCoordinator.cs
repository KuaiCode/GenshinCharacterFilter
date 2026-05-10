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
            await RestoreForShutdownAsync(cancellationToken);
            throw;
        }
        catch
        {
            await RestoreIfNeededAsync(CancellationToken.None);
            return;
        }

        if (IsTargetSpeaker(speaker))
        {
            await MuteIfNeededAsync(cancellationToken);
            return;
        }

        await RestoreIfNeededAsync(cancellationToken);
    }

    /// <summary>
    /// Attempts to restore audio during shutdown or cancellation cleanup.
    /// </summary>
    public async Task RestoreForShutdownAsync(CancellationToken cancellationToken)
    {
        // 取消已发出时仍要尝试恢复音频，避免清理操作被已取消的 token 立即跳过。
        CancellationToken cleanupToken = cancellationToken.IsCancellationRequested
            ? CancellationToken.None
            : cancellationToken;

        await RestoreIfNeededAsync(cleanupToken);
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

    private async Task RestoreIfNeededAsync(CancellationToken cancellationToken)
    {
        if (!ShouldAttemptRestore())
        {
            return;
        }

        State = MuteCoordinatorState.Restoring;

        try
        {
            await _audioMuteService.RestoreAsync(cancellationToken);
            State = MuteCoordinatorState.Idle;
        }
        catch
        {
            State = MuteCoordinatorState.Faulted;
            throw;
        }
    }

    private bool ShouldAttemptRestore()
    {
        return State is MuteCoordinatorState.Muted
            or MuteCoordinatorState.Restoring
            or MuteCoordinatorState.Faulted;
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
