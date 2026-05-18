using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Contains the result of applying the stability gate to one raw match.
/// </summary>
public sealed record DetectionStabilityResult(
    bool RawMatched,
    string? RawMatchedSpeaker,
    DetectionStableState StableState,
    DetectionStableState PreviousStableState,
    bool StableStateChanged,
    int ConsecutiveMatchCount,
    int ConsecutiveMissCount,
    SpeakerMatchKind RawMatchKind = SpeakerMatchKind.None);
