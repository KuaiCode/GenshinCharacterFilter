using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Tests;

public sealed class AppSettingsLoaderTests
{
    private const string DefaultChineseSpeaker = "\u6D41\u6D6A\u8005";

    [Fact]
    public void LoadDefault_ReturnsSafeDefaults()
    {
        AppSettings settings = new AppSettingsLoader().LoadDefault();

        Assert.False(settings.RealAudioEnabled);
        Assert.Equal("GenshinImpact", settings.TargetProcessName);
        Assert.Equal([DefaultChineseSpeaker, "Wanderer"], settings.TargetSpeakers);
        Assert.Equal(AudioFilterMode.Mute, settings.AudioFilter.Mode);
        Assert.Equal(30, settings.AudioFilter.VolumePercent);
        Assert.Equal(OcrEngine.TesseractCli, settings.Ocr.Engine);
        Assert.Equal("tesseract", settings.Ocr.TesseractExecutablePath);
        Assert.Null(settings.Ocr.PaddleModelDirectory);
        Assert.Null(settings.Ocr.PaddleRuntimeDirectory);
        Assert.Equal("chi_sim+eng", settings.Ocr.Language);
        Assert.Equal(7, settings.Ocr.PageSegmentationMode);
        Assert.Equal(1, settings.Ocr.InputScale);
        Assert.Equal(0, settings.Ocr.PaddingPixels);
        Assert.False(settings.Ocr.Grayscale);
        Assert.False(settings.Ocr.Invert);
        Assert.Null(settings.Ocr.Threshold);
        Assert.Null(settings.Ocr.Region);
        Assert.Null(settings.Ocr.RegionConfigPath);
        Assert.Null(settings.Ocr.RegionPreset);
        Assert.Equal(1000, settings.Detection.LoopIntervalMs);
        Assert.Null(settings.Detection.LoopCount);
        Assert.Equal(2, settings.Detection.MatchThreshold);
        Assert.Equal(2, settings.Detection.MissThreshold);
        Assert.False(settings.Detection.SaveDebugImages);
        Assert.False(settings.Detection.SaveOcrFailureSamples);
    }

    [Fact]
    public void LoadFromFile_ReadsValidJson()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "chrome",
              "TargetSpeakers": [ "Furina", "Wanderer" ],
              "RealAudioEnabled": true,
              "AudioFilter": {
                "Mode": "ReduceVolume",
                "VolumePercent": 25
              },
              "Ocr": {
                "Engine": "TesseractCli",
                "TesseractExecutablePath": "C:\\Tools\\tesseract.exe",
                "PaddleModelDirectory": "models",
                "PaddleRuntimeDirectory": "runtime",
                "Language": "chi_sim",
                "PageSegmentationMode": 7,
                "InputScale": 2,
                "PaddingPixels": 5,
                "Grayscale": true,
                "Invert": true,
                "Threshold": 120,
                "RegionConfigPath": "ocr-region.json",
                "RegionPreset": "none"
              },
              "Detection": {
                "LoopIntervalMs": 500,
                "LoopCount": 5,
                "MatchThreshold": 3,
                "MissThreshold": 4,
                "SaveDebugImages": true,
                "SaveOcrFailureSamples": true
              }
            }
            """);

        AppSettings settings = new AppSettingsLoader().LoadFromFile(configFile.Path);

        Assert.True(settings.RealAudioEnabled);
        Assert.Equal("chrome", settings.TargetProcessName);
        Assert.Equal(["Furina", "Wanderer"], settings.TargetSpeakers);
        Assert.Equal(AudioFilterMode.ReduceVolume, settings.AudioFilter.Mode);
        Assert.Equal(25, settings.AudioFilter.VolumePercent);
        Assert.Equal(OcrEngine.TesseractCli, settings.Ocr.Engine);
        Assert.Equal("C:\\Tools\\tesseract.exe", settings.Ocr.TesseractExecutablePath);
        Assert.Equal("models", settings.Ocr.PaddleModelDirectory);
        Assert.Equal("runtime", settings.Ocr.PaddleRuntimeDirectory);
        Assert.Equal("chi_sim", settings.Ocr.Language);
        Assert.Equal(7, settings.Ocr.PageSegmentationMode);
        Assert.Equal(2, settings.Ocr.InputScale);
        Assert.Equal(5, settings.Ocr.PaddingPixels);
        Assert.True(settings.Ocr.Grayscale);
        Assert.True(settings.Ocr.Invert);
        Assert.Equal(120, settings.Ocr.Threshold);
        Assert.Equal("ocr-region.json", settings.Ocr.RegionConfigPath);
        Assert.Equal("none", settings.Ocr.RegionPreset);
        Assert.Equal(500, settings.Detection.LoopIntervalMs);
        Assert.Equal(5, settings.Detection.LoopCount);
        Assert.Equal(3, settings.Detection.MatchThreshold);
        Assert.Equal(4, settings.Detection.MissThreshold);
        Assert.True(settings.Detection.SaveDebugImages);
        Assert.True(settings.Detection.SaveOcrFailureSamples);
    }

    [Fact]
    public void LoadFromFile_ReadsAbsoluteOcrRegion()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 },
              "Ocr": {
                "Region": { "X": 10, "Y": 20, "Width": 30, "Height": 40 }
              }
            }
            """);

        AppSettings settings = new AppSettingsLoader().LoadFromFile(configFile.Path);

        Assert.Equal(new OcrRegion(10, 20, 30, 40), settings.Ocr.Region);
    }

    [Fact]
    public void LoadFromFile_MissingFileThrowsClearError()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(missingPath));

        Assert.Contains("Config file not found", exception.Message);
    }

    [Fact]
    public void LoadFromFile_InvalidJsonThrowsClearError()
    {
        using TempJsonFile configFile = TempJsonFile.Create("{ not valid json");

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("invalid JSON", exception.Message);
    }

    [Fact]
    public void LoadFromFile_BlankTargetProcessNameIsRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": " ",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("TargetProcessName", exception.Message);
    }

    [Fact]
    public void LoadFromFile_EmptyTargetSpeakersIsRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("TargetSpeakers", exception.Message);
    }

    [Fact]
    public void LoadFromFile_BlankTargetSpeakerIsRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer", " " ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("empty speaker", exception.Message);
    }

    [Fact]
    public void LoadFromFile_InvalidAudioFilterModeIsRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "NotAMode", "VolumePercent": 30 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("invalid JSON", exception.Message);
    }

    [Fact]
    public void LoadFromFile_InvalidOcrPageSegmentationModeIsRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 },
              "Ocr": { "PageSegmentationMode": 99 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("PageSegmentationMode", exception.Message);
    }

    [Fact]
    public void LoadFromFile_AmbiguousOcrRegionSourcesAreRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 },
              "Ocr": {
                "Region": { "X": 1, "Y": 2, "Width": 3, "Height": 4 },
                "RegionConfigPath": "ocr-region.json"
              }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("--ocr-region", exception.Message);
    }

    [Fact]
    public void LoadFromFile_InvalidDetectionSettingsAreRejected()
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            """
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "Mute", "VolumePercent": 30 },
              "Detection": { "LoopIntervalMs": 1 }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("Loop interval", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void LoadFromFile_InvalidReduceVolumePercentIsRejected(int volumePercent)
    {
        using TempJsonFile configFile = TempJsonFile.Create(
            $$"""
            {
              "TargetProcessName": "GenshinImpact",
              "TargetSpeakers": [ "Wanderer" ],
              "RealAudioEnabled": false,
              "AudioFilter": { "Mode": "ReduceVolume", "VolumePercent": {{volumePercent}} }
            }
            """);

        AppSettingsException exception = Assert.Throws<AppSettingsException>(
            () => new AppSettingsLoader().LoadFromFile(configFile.Path));

        Assert.Contains("Volume percent", exception.Message);
    }

    [Fact]
    public void ConfigExample_IsSafeAndUsesExpectedTargetSpeakers()
    {
        string configPath = Path.Combine(FindRepositoryRoot(), "config.example.json");

        AppSettings settings = new AppSettingsLoader().LoadFromFile(configPath);

        Assert.False(settings.RealAudioEnabled);
        Assert.Equal([DefaultChineseSpeaker, "Wanderer"], settings.TargetSpeakers);
        Assert.Null(settings.Ocr.RegionConfigPath);
        Assert.Equal("none", settings.Ocr.RegionPreset);
        Assert.Equal(500, settings.Detection.LoopIntervalMs);
        Assert.False(settings.Detection.SaveDebugImages);
        Assert.False(settings.Detection.SaveOcrFailureSamples);
    }

    [Fact]
    public void GitIgnore_IgnoresLocalConfig()
    {
        string gitIgnorePath = Path.Combine(FindRepositoryRoot(), ".gitignore");
        string gitIgnore = File.ReadAllText(gitIgnorePath);

        Assert.Contains("config.local.json", gitIgnore);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GenshinCharacterFilter.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed class TempJsonFile : IDisposable
    {
        private TempJsonFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TempJsonFile Create(string content)
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
            File.WriteAllText(path, content);
            return new TempJsonFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
