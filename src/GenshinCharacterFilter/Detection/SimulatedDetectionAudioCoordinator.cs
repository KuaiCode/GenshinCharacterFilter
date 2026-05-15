using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Converts stable detection states into simulated audio requests.
/// </summary>
public sealed class SimulatedDetectionAudioCoordinator
{
    private readonly IAudioMuteService _audioMuteService;
    private bool _audioFiltered;

    public SimulatedDetectionAudioCoordinator(IAudioMuteService audioMuteService)
    {
        _audioMuteService = audioMuteService ?? throw new ArgumentNullException(nameof(audioMuteService));
    }

    /// <summary>
    /// Applies one stable detection result to the simulated audio service.
    /// </summary>
    public async Task<SimulatedAudioActionResult> ApplyAsync(
        DetectionStabilityResult stabilityResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stabilityResult);

        DetectionStableState stableState = stabilityResult.StableState;
        if (stableState.Matched)
        {
            if (string.IsNullOrWhiteSpace(stableState.MatchedSpeaker) || _audioFiltered)
            {
                return new SimulatedAudioActionResult(SimulatedAudioAction.None, _audioFiltered);
            }

            await _audioMuteService.MuteAsync(cancellationToken);
            _audioFiltered = true;
            return new SimulatedAudioActionResult(SimulatedAudioAction.Mute, _audioFiltered);
        }

        if (!_audioFiltered)
        {
            return new SimulatedAudioActionResult(SimulatedAudioAction.None, _audioFiltered);
        }

        await _audioMuteService.RestoreAsync(cancellationToken);
        _audioFiltered = false;
        return new SimulatedAudioActionResult(SimulatedAudioAction.Restore, _audioFiltered);
    }

    /// <summary>
    /// Restores simulated audio during shutdown if a simulated filter is active.
    /// </summary>
    public async Task<SimulatedAudioActionResult> RestoreForShutdownAsync(CancellationToken cancellationToken)
    {
        if (!_audioFiltered)
        {
            return new SimulatedAudioActionResult(SimulatedAudioAction.None, _audioFiltered);
        }

        await _audioMuteService.RestoreAsync(cancellationToken);
        _audioFiltered = false;
        return new SimulatedAudioActionResult(SimulatedAudioAction.Restore, _audioFiltered);
    }
}
