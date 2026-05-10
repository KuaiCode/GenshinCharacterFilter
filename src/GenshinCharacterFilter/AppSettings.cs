using GenshinCharacterFilter.Audio;

namespace GenshinCharacterFilter;

/// <summary>
/// Stores local application settings loaded from defaults, JSON, and CLI overrides.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the target process name used by real audio mode.
    /// </summary>
    public string TargetProcessName { get; set; } = "GenshinImpact";

    /// <summary>
    /// Gets or sets speaker names that should trigger audio filtering.
    /// </summary>
    public List<string> TargetSpeakers { get; set; } = ["派蒙", "Paimon"];

    /// <summary>
    /// Gets or sets whether real Windows audio mode is enabled.
    /// </summary>
    public bool RealAudioEnabled { get; set; }

    /// <summary>
    /// Gets or sets the configured audio filter behavior.
    /// </summary>
    public AudioFilterOptions AudioFilter { get; set; } = new();

    /// <summary>
    /// Creates a safe default configuration.
    /// </summary>
    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }

    /// <summary>
    /// Validates settings and normalizes whitespace around speaker names.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(TargetProcessName))
        {
            throw new AppSettingsException("TargetProcessName cannot be empty.");
        }

        if (TargetSpeakers is null || TargetSpeakers.Count == 0)
        {
            throw new AppSettingsException("TargetSpeakers must contain at least one speaker.");
        }

        List<string> normalizedSpeakers = [];
        foreach (string? speaker in TargetSpeakers)
        {
            if (string.IsNullOrWhiteSpace(speaker))
            {
                throw new AppSettingsException("TargetSpeakers cannot contain empty speaker names.");
            }

            string normalizedSpeaker = speaker.Trim();
            if (!normalizedSpeakers.Contains(normalizedSpeaker, StringComparer.OrdinalIgnoreCase))
            {
                normalizedSpeakers.Add(normalizedSpeaker);
            }
        }

        if (normalizedSpeakers.Count == 0)
        {
            throw new AppSettingsException("TargetSpeakers must contain at least one non-empty speaker.");
        }

        TargetProcessName = TargetProcessName.Trim();
        TargetSpeakers = normalizedSpeakers;

        if (AudioFilter is null)
        {
            throw new AppSettingsException("AudioFilter is required.");
        }

        try
        {
            AudioFilter.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new AppSettingsException(exception.Message, exception);
        }
    }
}
