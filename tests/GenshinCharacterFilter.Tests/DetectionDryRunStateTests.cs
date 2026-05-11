using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionDryRunStateTests
{
    private const string WandererChinese = "\u6D41\u6D6A\u8005";

    [Fact]
    public void IsStateChangeFrom_NotMatchedToMatched_ReturnsTrue()
    {
        DetectionDryRunState current = new(true, WandererChinese);

        Assert.True(current.IsStateChangeFrom(DetectionDryRunState.NotMatched));
    }

    [Fact]
    public void IsStateChangeFrom_MatchedToNotMatched_ReturnsTrue()
    {
        DetectionDryRunState previous = new(true, WandererChinese);

        Assert.True(DetectionDryRunState.NotMatched.IsStateChangeFrom(previous));
    }

    [Fact]
    public void IsStateChangeFrom_MatchedSameSpeaker_ReturnsFalse()
    {
        DetectionDryRunState previous = new(true, WandererChinese);
        DetectionDryRunState current = new(true, WandererChinese);

        Assert.False(current.IsStateChangeFrom(previous));
    }

    [Fact]
    public void IsStateChangeFrom_NotMatchedToNotMatched_ReturnsFalse()
    {
        Assert.False(DetectionDryRunState.NotMatched.IsStateChangeFrom(DetectionDryRunState.NotMatched));
    }

    [Fact]
    public void IsStateChangeFrom_NoPreviousState_ReturnsFalse()
    {
        Assert.False(DetectionDryRunState.NotMatched.IsStateChangeFrom(null));
    }

    [Fact]
    public void FromMatch_CreatesMatchedState()
    {
        SpeakerMatchResult matchResult = new(true, WandererChinese, WandererChinese, WandererChinese);

        DetectionDryRunState state = DetectionDryRunState.FromMatch(matchResult);

        Assert.True(state.Matched);
        Assert.Equal(WandererChinese, state.MatchedSpeaker);
    }

    [Fact]
    public void FromMatch_CreatesNotMatchedState()
    {
        SpeakerMatchResult matchResult = new(false, null, "unknown", "unknown");

        DetectionDryRunState state = DetectionDryRunState.FromMatch(matchResult);

        Assert.Equal(DetectionDryRunState.NotMatched, state);
    }
}
