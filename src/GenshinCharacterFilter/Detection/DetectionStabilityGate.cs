using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Converts raw speaker match results into a stable state using consecutive frame thresholds.
/// </summary>
public sealed class DetectionStabilityGate
{
    private readonly DetectionStabilityOptions _options;
    private string? _candidateMatchedSpeaker;

    public DetectionStabilityGate(DetectionStabilityOptions? options = null)
    {
        _options = options ?? new DetectionStabilityOptions();
        _options.Validate();
    }

    /// <summary>
    /// Gets the current stability-gated state.
    /// </summary>
    public DetectionStableState StableState { get; private set; } = DetectionStableState.NotMatched;

    /// <summary>
    /// Gets the consecutive raw match count for the current candidate speaker.
    /// </summary>
    public int ConsecutiveMatchCount { get; private set; }

    /// <summary>
    /// Gets the consecutive raw miss count.
    /// </summary>
    public int ConsecutiveMissCount { get; private set; }

    /// <summary>
    /// Applies one raw speaker match result and returns the updated stable state.
    /// </summary>
    public DetectionStabilityResult Observe(SpeakerMatchResult rawMatch)
    {
        ArgumentNullException.ThrowIfNull(rawMatch);

        DetectionStableState previousState = StableState;

        if (rawMatch.MatchKind == SpeakerMatchKind.Strong)
        {
            ObserveMatch(rawMatch.MatchedSpeaker);
        }
        else if (rawMatch.MatchKind == SpeakerMatchKind.Weak)
        {
            ObserveWeakMatch(rawMatch.MatchedSpeaker);
        }
        else if (rawMatch.MatchKind == SpeakerMatchKind.Unknown)
        {
            ObserveUnknownMiss();
        }
        else
        {
            ObserveMiss();
        }

        bool stateChanged = !Equals(previousState, StableState);
        return new DetectionStabilityResult(
            rawMatch.Matched,
            rawMatch.MatchedSpeaker,
            StableState,
            previousState,
            stateChanged,
            ConsecutiveMatchCount,
            ConsecutiveMissCount,
            rawMatch.MatchKind);
    }

    private void ObserveMatch(string? matchedSpeaker)
    {
        ConsecutiveMissCount = 0;

        if (!string.Equals(_candidateMatchedSpeaker, matchedSpeaker, StringComparison.Ordinal))
        {
            _candidateMatchedSpeaker = matchedSpeaker;
            ConsecutiveMatchCount = 0;
        }

        ConsecutiveMatchCount++;
        if (ConsecutiveMatchCount >= _options.MatchThreshold)
        {
            StableState = new DetectionStableState(true, matchedSpeaker);
        }
    }

    private void ObserveMiss()
    {
        _candidateMatchedSpeaker = null;
        ConsecutiveMatchCount = 0;
        ConsecutiveMissCount++;

        if (ConsecutiveMissCount >= _options.MissThreshold)
        {
            StableState = DetectionStableState.NotMatched;
        }
    }

    private void ObserveWeakMatch(string? matchedSpeaker)
    {
        _candidateMatchedSpeaker = null;
        ConsecutiveMatchCount = 0;
        ConsecutiveMissCount = 0;

        if (StableState.Matched &&
            string.Equals(StableState.MatchedSpeaker, matchedSpeaker, StringComparison.Ordinal))
        {
            return;
        }
    }

    private void ObserveUnknownMiss()
    {
        _candidateMatchedSpeaker = null;
        ConsecutiveMatchCount = 0;
        ConsecutiveMissCount++;

        // Empty or very noisy OCR should not immediately restore real audio after a stable hit.
        int requiredUnknownMisses = Math.Max(2, _options.MissThreshold);
        if (ConsecutiveMissCount >= requiredUnknownMisses)
        {
            StableState = DetectionStableState.NotMatched;
        }
    }
}
