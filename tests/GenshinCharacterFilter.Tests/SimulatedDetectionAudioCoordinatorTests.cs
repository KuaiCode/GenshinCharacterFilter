using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class SimulatedDetectionAudioCoordinatorTests
{
    [Fact]
    public async Task ApplyAsync_RawMatchBelowThresholdDoesNotCallMute()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        DetectionAudioActionResult result = await coordinator.ApplyAsync(
            gate.Observe(Matched("Wanderer")),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, result.Action);
        Assert.Equal(0, audio.MuteCalls);
    }

    [Fact]
    public async Task ApplyAsync_WeakMatchDoesNotCallMute()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = new(new DetectionStabilityOptions
        {
            MatchThreshold = 1,
            MissThreshold = 1
        });

        DetectionAudioActionResult result = await coordinator.ApplyAsync(
            gate.Observe(new SpeakerMatchResult(true, "Clorinde", "near", "near", SpeakerMatchKind.Weak)),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, result.Action);
        Assert.Equal(0, audio.MuteCalls);
    }

    [Fact]
    public async Task ApplyAsync_StableMatchedCallsMuteOnce()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        DetectionAudioActionResult result = await coordinator.ApplyAsync(
            gate.Observe(Matched("Wanderer")),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.Mute, result.Action);
        Assert.Equal(1, audio.MuteCalls);
    }

    [Fact]
    public async Task ApplyAsync_StableMatchedWithReduceModeReturnsReduceAction()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(
            audio,
            new AudioFilterOptions
            {
                Mode = AudioFilterMode.ReduceVolume,
                VolumePercent = 30
            });
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        DetectionAudioActionResult result = await coordinator.ApplyAsync(
            gate.Observe(Matched("Wanderer")),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.Reduce, result.Action);
        Assert.Equal(1, audio.MuteCalls);
    }

    [Fact]
    public async Task ApplyAsync_RepeatedStableMatchedDoesNotCallMuteRepeatedly()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        DetectionAudioActionResult repeated = await coordinator.ApplyAsync(
            gate.Observe(Matched("Wanderer")),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, repeated.Action);
        Assert.Equal(1, audio.MuteCalls);
    }

    [Fact]
    public async Task ApplyAsync_StableNotMatchedAfterMuteCallsRestoreOnce()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(NotMatched()), CancellationToken.None);
        DetectionAudioActionResult result = await coordinator.ApplyAsync(
            gate.Observe(NotMatched()),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.Restore, result.Action);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Fact]
    public async Task ApplyAsync_RepeatedStableNotMatchedDoesNotCallRestoreRepeatedly()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(NotMatched()), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(NotMatched()), CancellationToken.None);
        DetectionAudioActionResult repeated = await coordinator.ApplyAsync(
            gate.Observe(NotMatched()),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, repeated.Action);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyAsync_NullOrBlankStableMatchedSpeakerDoesNotCallMute(string? matchedSpeaker)
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityResult stabilityResult = new(
            true,
            matchedSpeaker,
            new DetectionStableState(true, matchedSpeaker),
            DetectionStableState.NotMatched,
            true,
            2,
            0);

        DetectionAudioActionResult result = await coordinator.ApplyAsync(stabilityResult, CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, result.Action);
        Assert.Equal(0, audio.MuteCalls);
    }

    [Fact]
    public async Task RestoreForShutdownAsync_AfterSimulatedMuteAttemptsRestore()
    {
        FakeAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        DetectionAudioActionResult result = await coordinator.RestoreForShutdownAsync(CancellationToken.None);

        Assert.Equal(DetectionAudioAction.Restore, result.Action);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Fact]
    public async Task RestoreForShutdownAsync_AfterPartialMuteFailureAttemptsRestore()
    {
        PartialFailureAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None));
        DetectionAudioActionResult result = await coordinator.RestoreForShutdownAsync(CancellationToken.None);

        Assert.True(audio.PartialApplyStarted);
        Assert.Equal(1, audio.MuteCalls);
        Assert.Equal(DetectionAudioAction.Restore, result.Action);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Fact]
    public async Task RestoreForShutdownAsync_AfterPartialMuteFailureDoesNotSpamAfterSuccessfulRestore()
    {
        PartialFailureAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None));
        await coordinator.RestoreForShutdownAsync(CancellationToken.None);
        DetectionAudioActionResult repeated = await coordinator.RestoreForShutdownAsync(CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, repeated.Action);
        Assert.Equal(1, audio.RestoreCalls);
    }

    [Fact]
    public async Task ApplyAsync_AfterPartialMuteFailureDoesNotCallMuteRepeatedlyBeforeRestore()
    {
        PartialFailureAudioMuteService audio = new();
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None));
        DetectionAudioActionResult repeated = await coordinator.ApplyAsync(
            gate.Observe(Matched("Wanderer")),
            CancellationToken.None);

        Assert.Equal(DetectionAudioAction.None, repeated.Action);
        Assert.Equal(1, audio.MuteCalls);
    }

    [Fact]
    public async Task RestoreForShutdownAsync_AfterRestoreFailureCanRetry()
    {
        PartialFailureAudioMuteService audio = new()
        {
            ThrowOnRestore = true
        };
        SimulatedDetectionAudioCoordinator coordinator = new(audio);
        DetectionStabilityGate gate = CreateGate();

        await coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.ApplyAsync(gate.Observe(Matched("Wanderer")), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => coordinator.RestoreForShutdownAsync(CancellationToken.None));
        audio.ThrowOnRestore = false;
        DetectionAudioActionResult retry = await coordinator.RestoreForShutdownAsync(CancellationToken.None);

        Assert.Equal(DetectionAudioAction.Restore, retry.Action);
        Assert.Equal(2, audio.RestoreCalls);
    }

    private static DetectionStabilityGate CreateGate()
    {
        return new DetectionStabilityGate(new DetectionStabilityOptions
        {
            MatchThreshold = 2,
            MissThreshold = 2
        });
    }

    private static SpeakerMatchResult Matched(string speaker)
    {
        return new SpeakerMatchResult(true, speaker, speaker, speaker);
    }

    private static SpeakerMatchResult NotMatched()
    {
        return new SpeakerMatchResult(false, null, "unknown", "unknown");
    }

    private sealed class FakeAudioMuteService : IAudioMuteService
    {
        public int MuteCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public Task MuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MuteCalls++;
            return Task.CompletedTask;
        }

        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RestoreCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class PartialFailureAudioMuteService : IAudioMuteService
    {
        public int MuteCalls { get; private set; }

        public int RestoreCalls { get; private set; }

        public bool PartialApplyStarted { get; private set; }

        public bool ThrowOnRestore { get; set; }

        public Task MuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MuteCalls++;
            PartialApplyStarted = true;
            throw new InvalidOperationException("Simulated partial audio apply failure.");
        }

        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RestoreCalls++;
            if (ThrowOnRestore)
            {
                throw new InvalidOperationException("Simulated restore failure.");
            }

            return Task.CompletedTask;
        }
    }
}
