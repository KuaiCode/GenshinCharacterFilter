using GenshinCharacterFilter.Detection;

namespace GenshinCharacterFilter;

/// <summary>
/// Stores detection-loop defaults loaded from local configuration.
/// </summary>
public sealed class AppDetectionSettings
{
    public int LoopIntervalMs { get; set; } = DetectionDryRunOptions.DefaultLoopIntervalMs;

    public int? LoopCount { get; set; }

    public int MatchThreshold { get; set; } = DetectionStabilityOptions.DefaultMatchThreshold;

    public int MissThreshold { get; set; } = DetectionStabilityOptions.DefaultMissThreshold;

    /// <summary>
    /// Validates detection timing and stability settings.
    /// </summary>
    public void Validate()
    {
        try
        {
            DetectionDryRunOptions.ValidateLoopIntervalMs(LoopIntervalMs);
            DetectionDryRunOptions.ValidateLoopCount(LoopCount);
            DetectionStabilityOptions.ValidateThreshold(MatchThreshold, nameof(MatchThreshold));
            DetectionStabilityOptions.ValidateThreshold(MissThreshold, nameof(MissThreshold));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new AppSettingsException(exception.Message, exception);
        }
    }
}
