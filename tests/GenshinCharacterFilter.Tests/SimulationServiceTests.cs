using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Tests;

public sealed class SimulationServiceTests
{
    [Fact]
    public async Task ManualSpeakerDetector_ReturnsLatestTrimmedSpeaker()
    {
        ManualSpeakerDetector detector = new();

        detector.SetSpeaker("  \u6D41\u6D6A\u8005  ");
        string? speaker = await detector.DetectSpeakerAsync(CancellationToken.None);

        Assert.Equal("\u6D41\u6D6A\u8005", speaker);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ManualSpeakerDetector_BlankSpeakerReturnsNull(string? input)
    {
        ManualSpeakerDetector detector = new();

        detector.SetSpeaker(input);
        string? speaker = await detector.DetectSpeakerAsync(CancellationToken.None);

        Assert.Null(speaker);
    }

    [Fact]
    public async Task LoggingAudioMuteService_WritesSimulatedRequests()
    {
        StringWriter writer = new();
        LoggingAudioMuteService service = new(writer);

        await service.MuteAsync(CancellationToken.None);
        await service.RestoreAsync(CancellationToken.None);

        string output = writer.ToString();
        Assert.Contains("[SIMULATED] Mute requested", output);
        Assert.Contains("[SIMULATED] Restore requested", output);
    }

    [Fact]
    public async Task LoggingAudioMuteService_WritesReduceRequest()
    {
        StringWriter writer = new();
        LoggingAudioMuteService service = new(
            writer,
            new AudioFilterOptions
            {
                Mode = AudioFilterMode.ReduceVolume,
                VolumePercent = 25
            });

        await service.MuteAsync(CancellationToken.None);

        Assert.Contains("[SIMULATED] Reduce volume to 25% requested", writer.ToString());
    }
}
