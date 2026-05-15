using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter.Tests;

public sealed class EffectiveConfigPrinterTests
{
    [Fact]
    public void Print_IncludesKeySafetyFields()
    {
        AppSettings settings = CreateSettings();
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--print-effective-config"]);
        StringWriter writer = new();

        new EffectiveConfigPrinter().Print(settings, options, writer);

        string output = writer.ToString();
        Assert.Contains("RealAudioEnabled: False", output);
        Assert.Contains("AllowRealAudioFromDetection: False", output);
        Assert.Contains("Detection real audio allowed: False", output);
        Assert.Contains("TargetProcessName: GenshinImpact", output);
        Assert.Contains("TargetSpeakers: Wanderer", output);
    }

    [Fact]
    public void Print_ShowsCliOverrideValues()
    {
        AppSettings settings = CreateSettings();
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--process", "chrome", "--audio-mode", "reduce", "--volume-percent", "25"]);
        AppSettings merged = options.ApplyOverrides(settings);
        StringWriter writer = new();

        new EffectiveConfigPrinter().Print(merged, options, writer);

        string output = writer.ToString();
        Assert.Contains("TargetProcessName: chrome", output);
        Assert.Contains("AudioFilter.Mode: ReduceVolume", output);
        Assert.Contains("AudioFilter.VolumePercent: 25", output);
    }

    [Fact]
    public void Print_ConfigRealAudioWithoutCliDoesNotAllowDetectionRealAudio()
    {
        AppSettings settings = CreateSettings();
        settings.RealAudioEnabled = true;
        AppCommandLineOptions options = AppCommandLineOptions.Parse(["--detect-loop", "--ocr-input", "input.png"]);
        AppSettings merged = options.ApplyOverrides(settings);
        StringWriter writer = new();

        new EffectiveConfigPrinter().Print(merged, options, writer);

        string output = writer.ToString();
        Assert.Contains("RealAudioEnabled: False", output);
        Assert.Contains("AllowRealAudioFromDetection: False", output);
        Assert.Contains("Detection real audio allowed: False", output);
    }

    private static AppSettings CreateSettings()
    {
        return new AppSettings
        {
            TargetProcessName = "GenshinImpact",
            TargetSpeakers = ["Wanderer"],
            AudioFilter = new AudioFilterOptions()
        };
    }
}
