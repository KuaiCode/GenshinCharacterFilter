using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Converts stable detection states into simulated audio requests.
/// </summary>
public sealed class SimulatedDetectionAudioCoordinator : DetectionAudioCoordinator
{
    public SimulatedDetectionAudioCoordinator(
        IAudioMuteService audioMuteService,
        AudioFilterOptions? audioFilterOptions = null)
        : base(audioMuteService, audioFilterOptions)
    {
    }
}
