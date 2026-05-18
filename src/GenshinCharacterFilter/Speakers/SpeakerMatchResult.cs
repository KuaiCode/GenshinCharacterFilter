namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Contains the result of a one-shot speaker text match.
/// </summary>
public sealed record SpeakerMatchResult
{
    public SpeakerMatchResult(bool matched, string? matchedSpeaker, string rawText, string normalizedText)
        : this(
            matched,
            matchedSpeaker,
            rawText,
            normalizedText,
            matched ? SpeakerMatchKind.Strong : SpeakerMatchKind.None)
    {
    }

    public SpeakerMatchResult(
        bool matched,
        string? matchedSpeaker,
        string rawText,
        string normalizedText,
        SpeakerMatchKind matchKind)
    {
        Matched = matched;
        MatchedSpeaker = matchedSpeaker;
        RawText = rawText;
        NormalizedText = normalizedText;
        MatchKind = matchKind;
    }

    public bool Matched { get; init; }

    public string? MatchedSpeaker { get; init; }

    public string RawText { get; init; }

    public string NormalizedText { get; init; }

    public SpeakerMatchKind MatchKind { get; init; }
}
