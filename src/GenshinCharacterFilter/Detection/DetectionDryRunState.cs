using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Represents the matched/not-matched state observed by the dry-run loop.
/// </summary>
public sealed record DetectionDryRunState(bool Matched, string? MatchedSpeaker)
{
    public static DetectionDryRunState NotMatched { get; } = new(false, null);

    /// <summary>
    /// Creates a dry-run state from a speaker match result.
    /// </summary>
    public static DetectionDryRunState FromMatch(SpeakerMatchResult matchResult)
    {
        ArgumentNullException.ThrowIfNull(matchResult);

        return matchResult.Matched
            ? new DetectionDryRunState(true, matchResult.MatchedSpeaker)
            : NotMatched;
    }

    /// <summary>
    /// Returns true when the observed matched state changed.
    /// </summary>
    public bool IsStateChangeFrom(DetectionDryRunState? previous)
    {
        return previous is not null && !Equals(previous);
    }

    public override string ToString()
    {
        return Matched
            ? $"Matched({MatchedSpeaker ?? "unknown"})"
            : "NotMatched";
    }
}
