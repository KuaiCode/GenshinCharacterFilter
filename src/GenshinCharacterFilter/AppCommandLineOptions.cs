using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter;

/// <summary>
/// Parses command-line options for the console application.
/// </summary>
public sealed class AppCommandLineOptions
{
    private bool _realAudioSpecified;
    private bool _targetProcessSpecified;
    private bool _audioModeSpecified;
    private bool _volumePercentSpecified;

    public string? ConfigPath { get; private init; }

    public bool UseRealAudio { get; private init; }

    public string TargetProcessName { get; private init; } = "GenshinImpact";

    public AudioFilterOptions AudioFilter { get; private init; } = new();

    /// <summary>
    /// Parses command-line arguments into application options.
    /// </summary>
    public static AppCommandLineOptions Parse(string[] arguments)
    {
        string? audioMode = GetOptionValue(arguments, "--audio-mode");
        string? volumePercentValue = GetOptionValue(arguments, "--volume-percent");
        bool audioModeSpecified = audioMode is not null;
        bool volumePercentSpecified = volumePercentValue is not null;

        AudioFilterMode mode = audioModeSpecified
            ? ParseAudioMode(audioMode!)
            : AudioFilterMode.Mute;
        int volumePercent = AudioFilterOptions.DefaultVolumePercent;

        if (volumePercentSpecified &&
            !int.TryParse(volumePercentValue, out volumePercent))
        {
            throw new ArgumentException("Volume percent must be a number.", nameof(arguments));
        }

        AudioFilterOptions audioFilterOptions = new()
        {
            Mode = mode,
            VolumePercent = volumePercent
        };
        audioFilterOptions.Validate();

        string? processName = GetOptionValue(arguments, "--process");

        return new AppCommandLineOptions
        {
            ConfigPath = GetOptionValue(arguments, "--config"),
            UseRealAudio = arguments.Any(argument => string.Equals(argument, "--real-audio", StringComparison.OrdinalIgnoreCase)),
            TargetProcessName = processName ?? "GenshinImpact",
            AudioFilter = audioFilterOptions,
            _realAudioSpecified = arguments.Any(argument => string.Equals(argument, "--real-audio", StringComparison.OrdinalIgnoreCase)),
            _targetProcessSpecified = processName is not null,
            _audioModeSpecified = audioModeSpecified,
            _volumePercentSpecified = volumePercentSpecified
        };
    }

    /// <summary>
    /// Applies explicit CLI overrides to loaded settings.
    /// </summary>
    public AppSettings ApplyOverrides(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        AppSettings merged = new()
        {
            TargetProcessName = _targetProcessSpecified ? TargetProcessName : settings.TargetProcessName,
            TargetSpeakers = [.. settings.TargetSpeakers],
            RealAudioEnabled = _realAudioSpecified || settings.RealAudioEnabled,
            AudioFilter = new AudioFilterOptions
            {
                Mode = _audioModeSpecified ? AudioFilter.Mode : settings.AudioFilter.Mode,
                VolumePercent = _volumePercentSpecified ? AudioFilter.VolumePercent : settings.AudioFilter.VolumePercent
            }
        };

        merged.Validate();
        return merged;
    }

    private static AudioFilterMode ParseAudioMode(string audioMode)
    {
        return audioMode.Trim().ToLowerInvariant() switch
        {
            "mute" => AudioFilterMode.Mute,
            "reduce" => AudioFilterMode.ReduceVolume,
            "reduce-volume" => AudioFilterMode.ReduceVolume,
            _ => throw new ArgumentException("Audio mode must be 'mute' or 'reduce'.", nameof(audioMode))
        };
    }

    private static string? GetOptionValue(string[] arguments, string optionName)
    {
        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(arguments[i], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[i + 1];
            }
        }

        return null;
    }
}
