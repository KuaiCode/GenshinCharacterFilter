using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Ocr;

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
    public void Parse_ReadsOcrOptions()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--ocr-once", "--ocr-input", "debug-captures/capture-latest.png", "--ocr-lang", "chi_sim+eng", "--tesseract-path", "C:\\Tools\\tesseract.exe", "--ocr-psm", "7"]);

        Assert.True(options.OcrOnce);
        Assert.Equal("debug-captures/capture-latest.png", options.OcrInputPath);
        Assert.Equal("chi_sim+eng", options.OcrLanguage);
        Assert.Equal("C:\\Tools\\tesseract.exe", options.TesseractExecutablePath);
        Assert.Equal(7, options.OcrPageSegmentationMode);
        Assert.Null(options.OcrRegion);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsOcrRegion()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--ocr-once", "--ocr-input", "input.png", "--ocr-region", "50,80,700,120"]);

        Assert.Equal(new OcrRegion(50, 80, 700, 120), options.OcrRegion);
    }

    [Fact]
    public void Parse_ReadsDetectSpeakerOnceAndSpeakerText()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-speaker-once", "--speaker-text", "\u6D41\u6D6A\u8005\uFF1A"]);

        Assert.True(options.DetectSpeakerOnce);
        Assert.Equal("\u6D41\u6D6A\u8005\uFF1A", options.SpeakerText);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_DetectSpeakerOnceCanUseOcrInput()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--ocr-once", "--detect-speaker-once", "--ocr-input", "input.png"]);

        Assert.True(options.OcrOnce);
        Assert.True(options.DetectSpeakerOnce);
        Assert.Equal("input.png", options.OcrInputPath);
        Assert.Null(options.SpeakerText);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsDetectLoopWithFixedImageInput()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--ocr-input", "input.png", "--ocr-region", "10,20,30,40"]);

        Assert.True(options.DetectLoop);
        Assert.Equal("input.png", options.OcrInputPath);
        Assert.Equal(new OcrRegion(10, 20, 30, 40), options.OcrRegion);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsSimulateAudioFromDetection()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--simulate-audio-from-detection", "--detect-loop", "--ocr-input", "input.png"]);

        Assert.True(options.SimulateAudioFromDetection);
        Assert.True(options.DetectLoop);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsDetectLoopWithProcessInput()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--process", "notepad", "--ocr-region", "10,20,30,40"]);

        Assert.True(options.DetectLoop);
        Assert.Equal("notepad", options.TargetProcessName);
        Assert.Equal(new OcrRegion(10, 20, 30, 40), options.OcrRegion);
        Assert.False(options.UseRealAudio);
    }

    [Fact]
    public void Parse_ReadsLoopIntervalMs()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--ocr-input", "input.png", "--loop-interval-ms", "500"]);

        Assert.True(options.DetectLoop);
        Assert.Equal(500, options.LoopIntervalMs);
    }

    [Fact]
    public void Parse_ReadsLoopCount()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--ocr-input", "input.png", "--loop-count", "5"]);

        Assert.True(options.DetectLoop);
        Assert.Equal(5, options.LoopCount);
    }

    [Fact]
    public void Parse_ReadsMatchThreshold()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--ocr-input", "input.png", "--match-threshold", "3"]);

        Assert.True(options.DetectLoop);
        Assert.Equal(3, options.MatchThreshold);
    }

    [Fact]
    public void Parse_ReadsMissThreshold()
    {
        AppCommandLineOptions options = AppCommandLineOptions.Parse(
            ["--detect-loop", "--ocr-input", "input.png", "--miss-threshold", "4"]);

        Assert.True(options.DetectLoop);
        Assert.Equal(4, options.MissThreshold);
    }

    [Fact]
    public void Parse_OcrOnceRequiresInput()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--ocr-once"]));

        Assert.Contains("--ocr-input", exception.Message);
    }

    [Fact]
    public void Parse_DetectSpeakerOnceRequiresTextOrOcr()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--detect-speaker-once"]));

        Assert.Contains("--speaker-text", exception.Message);
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
    [InlineData("--ocr-input")]
    [InlineData("--ocr-lang")]
    [InlineData("--tesseract-path")]
    [InlineData("--ocr-psm")]
    [InlineData("--ocr-region")]
    [InlineData("--speaker-text")]
    [InlineData("--loop-interval-ms")]
    [InlineData("--loop-count")]
    [InlineData("--match-threshold")]
    [InlineData("--miss-threshold")]
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

    [Fact]
    public void Parse_RejectsNonNumericOcrPsm()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--ocr-psm", "abc"]));

        Assert.Contains("OCR page segmentation", exception.Message);
    }

    [Fact]
    public void Parse_RejectsNonNumericLoopInterval()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--loop-interval-ms", "abc"]));

        Assert.Contains("Loop interval", exception.Message);
    }

    [Theory]
    [InlineData("99")]
    [InlineData("10001")]
    public void Parse_RejectsLoopIntervalOutsideRange(string loopIntervalMs)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppCommandLineOptions.Parse(["--loop-interval-ms", loopIntervalMs]));
    }

    [Fact]
    public void Parse_RejectsNonNumericLoopCount()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--loop-count", "abc"]));

        Assert.Contains("Loop count", exception.Message);
    }

    [Theory]
    [InlineData("--match-threshold")]
    [InlineData("--miss-threshold")]
    public void Parse_RejectsNonNumericStabilityThreshold(string optionName)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse([optionName, "abc"]));

        Assert.Contains("must be a number", exception.Message);
    }

    [Theory]
    [InlineData("--match-threshold", "0")]
    [InlineData("--match-threshold", "11")]
    [InlineData("--miss-threshold", "0")]
    [InlineData("--miss-threshold", "11")]
    public void Parse_RejectsStabilityThresholdOutsideRange(string optionName, string threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppCommandLineOptions.Parse([optionName, threshold]));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_RejectsLoopCountOutsideRange(string loopCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppCommandLineOptions.Parse(["--loop-count", loopCount]));
    }

    [Fact]
    public void Parse_DetectLoopRequiresInputImageOrProcess()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--detect-loop"]));

        Assert.Contains("--ocr-input", exception.Message);
        Assert.Contains("--process", exception.Message);
    }

    [Fact]
    public void Parse_DetectLoopProcessModeRequiresOcrRegion()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--detect-loop", "--process", "notepad"]));

        Assert.Contains("--ocr-region", exception.Message);
    }

    [Fact]
    public void Parse_SimulateAudioFromDetectionRequiresDetectLoop()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--simulate-audio-from-detection"]));

        Assert.Contains("--detect-loop", exception.Message);
    }

    [Fact]
    public void Parse_RejectsSimulateAudioFromDetectionWithRealAudio()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => AppCommandLineOptions.Parse(
                ["--simulate-audio-from-detection", "--detect-loop", "--ocr-input", "input.png", "--real-audio"]));

        Assert.Contains("--real-audio", exception.Message);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("14")]
    public void Parse_RejectsOcrPsmOutsideRange(string pageSegmentationMode)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AppCommandLineOptions.Parse(["--ocr-psm", pageSegmentationMode]));
    }

    [Theory]
    [InlineData("1,2,3")]
    [InlineData("1,2,3,4,5")]
    [InlineData("1,two,3,4")]
    [InlineData("-1,2,3,4")]
    [InlineData("1,-2,3,4")]
    [InlineData("1,2,0,4")]
    [InlineData("1,2,3,0")]
    public void Parse_RejectsInvalidOcrRegion(string region)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => AppCommandLineOptions.Parse(["--ocr-region", region]));
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
            TargetSpeakers = ["Wanderer"],
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
            TargetSpeakers = ["Wanderer"],
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
            TargetSpeakers = ["Wanderer"],
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
            TargetSpeakers = ["Wanderer"],
            RealAudioEnabled = true,
            AudioFilter = new AudioFilterOptions()
        };
        AppCommandLineOptions options = AppCommandLineOptions.Parse([]);

        AppSettings merged = options.ApplyOverrides(settings);

        Assert.True(merged.RealAudioEnabled);
    }
}
