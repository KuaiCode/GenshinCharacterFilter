namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Contains the audio action requested for one stable detection state.
/// </summary>
public sealed record DetectionAudioActionResult(
    DetectionAudioAction Action,
    bool AudioFiltered);
