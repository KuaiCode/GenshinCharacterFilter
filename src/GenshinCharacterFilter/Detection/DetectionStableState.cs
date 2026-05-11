namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Represents the stability-gated detection state observed by the dry-run loop.
/// </summary>
public sealed record DetectionStableState(bool Matched, string? MatchedSpeaker)
{
    public static DetectionStableState NotMatched { get; } = new(false, null);

    public override string ToString()
    {
        return Matched
            ? $"Matched({MatchedSpeaker ?? "unknown"})"
            : "NotMatched";
    }
}
