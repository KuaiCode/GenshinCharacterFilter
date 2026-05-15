namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Contains the simulated audio action requested for one stable detection state.
/// </summary>
public sealed record SimulatedAudioActionResult(
    SimulatedAudioAction Action,
    bool AudioFiltered);
