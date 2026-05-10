using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Coordination;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class MuteCoordinatorTests
{
    [Fact]
    public async Task TargetSpeakerStartsSpeaking_MuteCalledOnce()
    {
        FakeSpeakerDetector detector = new("Paimon");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(0, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Muted, coordinator.State);
    }

    [Fact]
    public async Task RepeatedTargetSpeaker_MuteNotCalledRepeatedly()
    {
        FakeSpeakerDetector detector = new("Paimon", "Paimon", "paimon");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(0, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Muted, coordinator.State);
    }

    [Fact]
    public async Task NonTargetSpeakerWhileIdle_RestoreNotCalledRepeatedly()
    {
        FakeSpeakerDetector detector = new("Traveler", "Venti", "Unknown");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(0, audio.MuteCallCount);
        Assert.Equal(0, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Idle, coordinator.State);
    }

    [Fact]
    public async Task TargetThenNonTarget_MuteOnceAndRestoreOnce()
    {
        FakeSpeakerDetector detector = new("Paimon", "Traveler");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(1, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Idle, coordinator.State);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NullOrEmptySpeakerWhileMuted_RestoreOnce(string? speaker)
    {
        FakeSpeakerDetector detector = new("Paimon", speaker, speaker);
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(1, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Idle, coordinator.State);
    }

    [Fact]
    public async Task DetectorExceptionWhileMuted_RestoreAttemptedAndStateFaulted()
    {
        FakeSpeakerDetector detector = new("Paimon");
        detector.EnqueueError(new InvalidOperationException("Simulated detection failure."));
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(1, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Faulted, coordinator.State);
    }

    [Fact]
    public async Task RestoreIsIdempotent()
    {
        FakeSpeakerDetector detector = new("Paimon", "Traveler", "Traveler");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.RestoreCallCount);
        Assert.Equal(MuteCoordinatorState.Idle, coordinator.State);
    }

    [Fact]
    public async Task MuteIsIdempotent()
    {
        FakeSpeakerDetector detector = new("Paimon", "Paimon");
        FakeAudioMuteService audio = new();
        MuteCoordinator coordinator = CreateCoordinator(detector, audio);

        await coordinator.TickAsync(CancellationToken.None);
        await coordinator.TickAsync(CancellationToken.None);

        Assert.Equal(1, audio.MuteCallCount);
        Assert.Equal(MuteCoordinatorState.Muted, coordinator.State);
    }

    private static MuteCoordinator CreateCoordinator(
        ISpeakerDetector detector,
        IAudioMuteService audioMuteService)
    {
        return new MuteCoordinator(
            detector,
            audioMuteService,
            new MuteCoordinatorOptions
            {
                TargetSpeakers = new HashSet<string> { "Paimon" }
            });
    }

    private sealed class FakeAudioMuteService : IAudioMuteService
    {
        public int MuteCallCount { get; private set; }

        public int RestoreCallCount { get; private set; }

        public Task MuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MuteCallCount++;
            return Task.CompletedTask;
        }

        public Task RestoreAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RestoreCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSpeakerDetector : ISpeakerDetector
    {
        private readonly Queue<object?> _results;

        public FakeSpeakerDetector(params string?[] speakers)
        {
            _results = new Queue<object?>(speakers);
        }

        public void EnqueueError(Exception exception)
        {
            _results.Enqueue(exception);
        }

        public Task<string?> DetectSpeakerAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            object? result = _results.Count > 0 ? _results.Dequeue() : null;

            if (result is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((string?)result);
        }
    }
}
