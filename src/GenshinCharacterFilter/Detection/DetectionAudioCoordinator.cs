using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Converts stable detection states into audio service requests.
/// </summary>
public class DetectionAudioCoordinator
{
    private readonly IAudioMuteService _audioMuteService;
    private readonly DetectionAudioAction _filterAction;
    private bool _audioFiltered;
    private bool _restoreMayBeNeeded;

    public DetectionAudioCoordinator(IAudioMuteService audioMuteService, AudioFilterOptions? audioFilterOptions = null)
    {
        _audioMuteService = audioMuteService ?? throw new ArgumentNullException(nameof(audioMuteService));
        AudioFilterOptions options = audioFilterOptions ?? new AudioFilterOptions();
        options.Validate();
        _filterAction = options.Mode == AudioFilterMode.ReduceVolume
            ? DetectionAudioAction.Reduce
            : DetectionAudioAction.Mute;
    }

    /// <summary>
    /// Applies one stable detection result to the configured audio service.
    /// </summary>
    public async Task<DetectionAudioActionResult> ApplyAsync(
        DetectionStabilityResult stabilityResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stabilityResult);

        DetectionStableState stableState = stabilityResult.StableState;
        if (stableState.Matched)
        {
            if (string.IsNullOrWhiteSpace(stableState.MatchedSpeaker) ||
                _audioFiltered ||
                _restoreMayBeNeeded)
            {
                return new DetectionAudioActionResult(DetectionAudioAction.None, _audioFiltered);
            }

            // MuteAsync 可能已部分修改音频后才失败，因此从调用开始就允许 shutdown 恢复。
            _restoreMayBeNeeded = true;
            await _audioMuteService.MuteAsync(cancellationToken);
            _audioFiltered = true;
            return new DetectionAudioActionResult(_filterAction, _audioFiltered);
        }

        if (!_audioFiltered && !_restoreMayBeNeeded)
        {
            return new DetectionAudioActionResult(DetectionAudioAction.None, _audioFiltered);
        }

        await _audioMuteService.RestoreAsync(cancellationToken);
        _audioFiltered = false;
        _restoreMayBeNeeded = false;
        return new DetectionAudioActionResult(DetectionAudioAction.Restore, _audioFiltered);
    }

    /// <summary>
    /// Restores audio during shutdown if a detection-driven filter is active.
    /// </summary>
    public async Task<DetectionAudioActionResult> RestoreForShutdownAsync(CancellationToken cancellationToken)
    {
        if (!_audioFiltered && !_restoreMayBeNeeded)
        {
            return new DetectionAudioActionResult(DetectionAudioAction.None, _audioFiltered);
        }

        await _audioMuteService.RestoreAsync(cancellationToken);
        _audioFiltered = false;
        _restoreMayBeNeeded = false;
        return new DetectionAudioActionResult(DetectionAudioAction.Restore, _audioFiltered);
    }
}
