using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Tests;

public sealed class AppCommandLineOptionsTests
{
    [Fact]
    public void Parse_DefaultsToSimulatedMute()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse([]);

        Assert.False(options.UseRealAudio);
        Assert.Equal("GenshinImpact", options.TargetProcessName);
        Assert.Equal(AudioFilterMode.Mute, options.AudioFilter.Mode);
    }

    [Fact]
    public void Parse_ReadsRealAudioProcessAndReduceVolume()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--real-audio", "--process", "YuanShen.exe", "--audio-mode", "reduce", "--volume-percent", "25"]);

        Assert.True(options.UseRealAudio);
        Assert.Equal("YuanShen.exe", options.TargetProcessName);
        Assert.Equal(AudioFilterMode.ReduceVolume, options.AudioFilter.Mode);
        Assert.Equal(25, options.AudioFilter.VolumePercent);
    }

    [Fact]
    public void Parse_ReadsConfigPath()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--config", "config.example.json"]);

        Assert.Equal("config.example.json", options.ConfigPath);
    }

    [Fact]
    public void Parse_ReadsCaptureOnceAndOutputDirectory()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--capture-once", "--process", "chrome", "--capture-output", "debug-captures"]);

        Assert.True(options.CaptureOnce);
        Assert.Equal("chrome", options.TargetProcessName);
        Assert.Equal("debug-captures", options.CaptureOutputDirectory);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsCaptureDelayMs()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--capture-once", "--capture-delay-ms", "500"]);

        Assert.True(options.CaptureOnce);
        Assert.Equal(500, options.CaptureDelayMs);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsMuteAudioMode()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--audio-mode", "mute"]);

        Assert.Equal(AudioFilterMode.Mute, options.AudioFilter.Mode);
    }

    [Theory]
    [InlineData("loud")]
    [InlineData("")]
    public void Parse_RejectsInvalidAudioMode(string audioMode)
    {
        Assert.Throws<ArgumentException>(() => AppCommandLineOptions.Parse(["--audio-mode", audioMode]));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("101")]
    [InlineData("abc")]
    public void Parse_RejectsInvalidVolumePercent(string volumePercent)
    {
        Assert.ThrowsAny<ArgumentException>(() => AppCommandLineOptions.Parse(["--audio-mode", "reduce", "--volume-percent", volumePercent]));
    }

    [Theory]
    [InlineData("--config")]
    [InlineData("--process")]
    [InlineData("--audio-mode")]
    [InlineData("--volume-percent")]
    [InlineData("--capture-output")]
    [InlineData("--capture-delay-ms")]
    public void Parse_RejectsMissingOptionValue(string optionName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse([optionName]));

        Assert.Contains("requires a value", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNonNumericCaptureDelay()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--capture-delay-ms", "abc"]));

        Assert.Contains("Capture delay", exception.Message);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("5001")]
    public void Parse_RejectsCaptureDelayOutsideRange(string delay)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppCommandLineOptions.Parse(["--capture-delay-ms", delay]));
    }

    [Fact]
    public void Parse_RejectsOptionNameWhereValueIsRequired()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--config", "--process"]));

        Assert.Contains("requires a value", exception.Message);
    }

    [Fact]
    public void ApplyOverrides_CanOverrideProcessName()
    {
        AppSettings settings = new()
        {
            TargetProcessName = "chrome",
            TargetSpeakers = ["Paimon"],
            RealAudioEnabled = false,
            AudioFilter = new AudioFilterOptions()
        };
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--process", "GenshinImpact"]);

        AppSettings merged = options.ApplyOverrides(settings);

        Assert.Equal("GenshinImpact", merged.TargetProcessName);
        Assert.False(merged.RealAudioEnabled);
    }

    [Fact]
    public void ApplyOverrides_CanOverrideAudioModeAndVolumePercent()
    {
        AppSettings settings = new()
        {
            TargetProcessName = "chrome",
            TargetSpeakers = ["Paimon"],
            RealAudioEnabled = false,
            AudioFilter = new AudioFilterOptions
            {
                Mode = AudioFilterMode.Mute,
                VolumePercent = 30
            }
        };
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--audio-mode", "reduce", "--volume-percent", "20"]);

        AppSettings merged = options.ApplyOverrides(settings);

        Assert.Equal(AudioFilterMode.ReduceVolume, merged.AudioFilter.Mode);
        Assert.Equal(20, merged.AudioFilter.VolumePercent);
    }

    [Fact]
    public void ApplyOverrides_RealAudioFlagCanOnlyEnable()
    {
        AppSettings settings = new()
        {
            TargetProcessName = "chrome",
            TargetSpeakers = ["Paimon"],
            RealAudioEnabled = false,
            AudioFilter = new AudioFilterOptions()
        };
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--real-audio"]);

        AppSettings merged = options.ApplyOverrides(settings);

        Assert.True(merged.RealAudioEnabled);
    }

    [Fact]
    public void ApplyOverrides_PreservesRealAudioEnabledWhenCliOmitsRealAudio()
    {
        AppSettings settings = new()
        {
            TargetProcessName = "chrome",
            TargetSpeakers = ["Paimon"],
            RealAudioEnabled = true,
            AudioFilter = new AudioFilterOptions()
        };
        AppCommandLineOptions options = AppCommandLineOptions.Parse([]);

        AppSettings merged = options.ApplyOverrides(settings);

        Assert.True(merged.RealAudioEnabled);
    }
}
