using GenshinCharacterFilter;
using GenshinCharacterFilter.Audio;

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
              }
            }
            """);

        AppSettings settings = new AppSettingsLoader().LoadFromFile(configFile.Path);

        Assert.True(settings.RealAudioEnabled);
        Assert.Equal("chrome", settings.TargetProcessName);
        Assert.Equal(["Furina", "Wanderer"], settings.TargetSpeakers);
        Assert.Equal(AudioFilterMode.ReduceVolume, settings.AudioFilter.Mode);
        Assert.Equal(25, settings.AudioFilter.VolumePercent);
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
