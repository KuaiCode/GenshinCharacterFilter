using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Tests;

public sealed class AudioFilterOptionsTests
{
    [Fact]
    public void DefaultsToMuteMode()
    {
        AudioFilterOptions options = new();

        Assert.Equal(AudioFilterMode.Mute, options.Mode);
        Assert.Equal(AudioFilterOptions.DefaultVolumePercent, options.VolumePercent);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(100)]
    public void Validate_AllowsVolumePercentRange(int volumePercent)
    {
        AudioFilterOptions options = new()
        {
            Mode = AudioFilterMode.ReduceVolume,
            VolumePercent = volumePercent
        };

        options.Validate();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_RejectsVolumePercentOutsideRange(int volumePercent)
    {
        AudioFilterOptions options = new()
        {
            Mode = AudioFilterMode.ReduceVolume,
            VolumePercent = volumePercent
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    [Fact]
    public void Validate_RejectsUnsupportedMode()
    {
        AudioFilterOptions options = new()
        {
            Mode = (AudioFilterMode)999
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }
}
