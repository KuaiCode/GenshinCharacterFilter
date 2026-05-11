namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Contains the result of a one-shot speaker text match.
/// </summary>
public sealed record SpeakerMatchResult(
    bool Matched,
    string? MatchedSpeaker,
    string RawText,
    string NormalizedText);
