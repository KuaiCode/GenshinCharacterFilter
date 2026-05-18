using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class DetectionStabilityGateTests
{
    [Fact]
    public void Observe_SingleMatchDoesNotTriggerStableMatchedWhenThresholdIsTwo()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);

        DetectionStabilityResult result = gate.Observe(Matched("A"));

        Assert.False(result.StableState.Matched);
        Assert.False(result.StableStateChanged);
        Assert.Equal(1, result.ConsecutiveMatchCount);
        Assert.Equal(0, result.ConsecutiveMissCount);
    }

    [Fact]
    public void Observe_ConsecutiveMatchesTriggerStableMatched()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);

        gate.Observe(Matched("A"));
        DetectionStabilityResult result = gate.Observe(Matched("A"));

        Assert.True(result.StableState.Matched);
        Assert.Equal("A", result.StableState.MatchedSpeaker);
        Assert.True(result.StableStateChanged);
    }

    [Fact]
    public void Observe_ConsecutiveMissesTriggerStableNotMatched()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 1, missThreshold: 2);
        gate.Observe(Matched("A"));

        DetectionStabilityResult firstMiss = gate.Observe(NotMatched());
        DetectionStabilityResult secondMiss = gate.Observe(NotMatched());

        Assert.True(firstMiss.StableState.Matched);
        Assert.False(firstMiss.StableStateChanged);
        Assert.False(secondMiss.StableState.Matched);
        Assert.True(secondMiss.StableStateChanged);
    }

    [Fact]
    public void Observe_WeakMatchAfterStableMatchedDoesNotRestore()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 1, missThreshold: 1);
        gate.Observe(Matched("Clorinde"));

        DetectionStabilityResult result = gate.Observe(WeakMatched("Clorinde", "\u5492\u6D1B\u7433\u5FB7"));

        Assert.True(result.StableState.Matched);
        Assert.Equal("Clorinde", result.StableState.MatchedSpeaker);
        Assert.False(result.StableStateChanged);
        Assert.Equal(0, result.ConsecutiveMissCount);
    }

    [Fact]
    public void Observe_UnknownMissAfterStableMatchedDoesNotImmediatelyRestore()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 1, missThreshold: 1);
        gate.Observe(Matched("Clorinde"));

        DetectionStabilityResult result = gate.Observe(UnknownMiss());

        Assert.True(result.StableState.Matched);
        Assert.Equal("Clorinde", result.StableState.MatchedSpeaker);
        Assert.False(result.StableStateChanged);
        Assert.Equal(1, result.ConsecutiveMissCount);
    }

    [Fact]
    public void Observe_StrongMissAfterStableMatchedCanRestoreWithMissThresholdOne()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 1, missThreshold: 1);
        gate.Observe(Matched("Clorinde"));

        DetectionStabilityResult result = gate.Observe(NotMatched("\u91CD\u4E91"));

        Assert.False(result.StableState.Matched);
        Assert.True(result.StableStateChanged);
    }

    [Fact]
    public void Observe_WeakMatchDoesNotTriggerNewStableMatched()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 1, missThreshold: 1);

        DetectionStabilityResult result = gate.Observe(WeakMatched("Clorinde", "\u5492\u6D1B\u7433\u5FB7"));

        Assert.False(result.StableState.Matched);
        Assert.False(result.StableStateChanged);
        Assert.Equal(0, result.ConsecutiveMatchCount);
    }

    [Fact]
    public void Observe_RepeatedMissesWhileAlreadyNotMatchedDoNotEmitStateChanged()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);

        DetectionStabilityResult firstMiss = gate.Observe(NotMatched());
        DetectionStabilityResult secondMiss = gate.Observe(NotMatched());
        DetectionStabilityResult thirdMiss = gate.Observe(NotMatched());

        Assert.False(firstMiss.StableState.Matched);
        Assert.False(firstMiss.StableStateChanged);
        Assert.False(secondMiss.StableState.Matched);
        Assert.False(secondMiss.StableStateChanged);
        Assert.False(thirdMiss.StableState.Matched);
        Assert.False(thirdMiss.StableStateChanged);
    }

    [Fact]
    public void Observe_SameStableSpeakerDoesNotRepeatedlyEmitStateChanged()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);
        gate.Observe(Matched("A"));
        gate.Observe(Matched("A"));

        DetectionStabilityResult result = gate.Observe(Matched("A"));

        Assert.True(result.StableState.Matched);
        Assert.Equal("A", result.StableState.MatchedSpeaker);
        Assert.False(result.StableStateChanged);
    }

    [Fact]
    public void Observe_SpeakerSwitchRequiresConsecutiveNewSpeakerMatches()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);
        gate.Observe(Matched("A"));
        gate.Observe(Matched("A"));

        DetectionStabilityResult firstB = gate.Observe(Matched("B"));
        DetectionStabilityResult secondB = gate.Observe(Matched("B"));

        Assert.True(firstB.StableState.Matched);
        Assert.Equal("A", firstB.StableState.MatchedSpeaker);
        Assert.False(firstB.StableStateChanged);
        Assert.True(secondB.StableState.Matched);
        Assert.Equal("B", secondB.StableState.MatchedSpeaker);
        Assert.True(secondB.StableStateChanged);
    }

    [Fact]
    public void Observe_MissCountResetsAfterMatchedFrame()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);

        gate.Observe(NotMatched());
        DetectionStabilityResult result = gate.Observe(Matched("A"));

        Assert.Equal(1, result.ConsecutiveMatchCount);
        Assert.Equal(0, result.ConsecutiveMissCount);
    }

    [Fact]
    public void Observe_MatchCountResetsAfterMissFrame()
    {
        DetectionStabilityGate gate = CreateGate(matchThreshold: 2, missThreshold: 2);

        gate.Observe(Matched("A"));
        DetectionStabilityResult result = gate.Observe(NotMatched());

        Assert.Equal(0, result.ConsecutiveMatchCount);
        Assert.Equal(1, result.ConsecutiveMissCount);
    }

    private static DetectionStabilityGate CreateGate(int matchThreshold, int missThreshold)
    {
        return new DetectionStabilityGate(new DetectionStabilityOptions
        {
            MatchThreshold = matchThreshold,
            MissThreshold = missThreshold
        });
    }

    private static SpeakerMatchResult Matched(string speaker)
    {
        return new SpeakerMatchResult(true, speaker, speaker, speaker);
    }

    private static SpeakerMatchResult WeakMatched(string speaker, string rawText)
    {
        return new SpeakerMatchResult(true, speaker, rawText, rawText, SpeakerMatchKind.Weak);
    }

    private static SpeakerMatchResult UnknownMiss()
    {
        return new SpeakerMatchResult(false, null, string.Empty, string.Empty, SpeakerMatchKind.Unknown);
    }

    private static SpeakerMatchResult NotMatched(string rawText = "unknown")
    {
        return new SpeakerMatchResult(false, null, rawText, rawText);
    }
}
