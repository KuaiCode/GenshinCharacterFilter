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
    public List<string> TargetSpeakers { get; set; } = ["\u6D41\u6D6A\u8005", "Wanderer"];

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

        // 先清理用户输入的空白，再去重，避免同一角色因多余空格被重复匹配。
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

        // 进程名也做 trim，避免配置文件中的空格导致真实音频会话匹配失败。
        TargetProcessName = TargetProcessName.Trim();
        TargetSpeakers = normalizedSpeakers;

        if (AudioFilter is null)
        {
            throw new AppSettingsException("AudioFilter is required.");
        }

        try
        {
            // 音频参数在进入服务层前统一校验，避免非法百分比影响真实音频恢复。
            AudioFilter.Validate();
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new AppSettingsException(exception.Message, exception);
        }
    }
}
