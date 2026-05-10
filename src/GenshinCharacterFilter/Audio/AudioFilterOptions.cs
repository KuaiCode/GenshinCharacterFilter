namespace GenshinCharacterFilter.Audio;

/// <summary>
/// Configures the audio action used when a target speaker is detected.
/// </summary>
public sealed class AudioFilterOptions
{
    public const int DefaultVolumePercent = 30;

    /// <summary>
    /// Gets the filter mode to apply.
    /// </summary>
    public AudioFilterMode Mode { get; init; } = AudioFilterMode.Mute;

    /// <summary>
    /// Gets the target volume percentage used by reduce-volume mode.
    /// </summary>
    public int VolumePercent { get; init; } = DefaultVolumePercent;

    /// <summary>
    /// Validates option values before they are used by audio services.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Mode))
        {
            throw new ArgumentOutOfRangeException(nameof(Mode), "Audio filter mode is not supported.");
        }

        if (VolumePercent is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(VolumePercent), "Volume percent must be between 1 and 100.");
        }
    }
}
