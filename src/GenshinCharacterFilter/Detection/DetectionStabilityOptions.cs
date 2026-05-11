namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Options for converting raw OCR speaker matches into a stable dry-run state.
/// </summary>
public sealed class DetectionStabilityOptions
{
    public const int DefaultMatchThreshold = 2;
    public const int DefaultMissThreshold = 2;
    public const int MinThreshold = 1;
    public const int MaxThreshold = 10;

    /// <summary>
    /// Gets or sets the required consecutive raw matches before stable matched state.
    /// </summary>
    public int MatchThreshold { get; set; } = DefaultMatchThreshold;

    /// <summary>
    /// Gets or sets the required consecutive raw misses before stable not-matched state.
    /// </summary>
    public int MissThreshold { get; set; } = DefaultMissThreshold;

    /// <summary>
    /// Validates stability thresholds.
    /// </summary>
    public void Validate()
    {
        ValidateThreshold(MatchThreshold, nameof(MatchThreshold));
        ValidateThreshold(MissThreshold, nameof(MissThreshold));
    }

    /// <summary>
    /// Validates a single stability threshold value.
    /// </summary>
    public static void ValidateThreshold(int threshold, string parameterName)
    {
        if (threshold is < MinThreshold or > MaxThreshold)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Detection stability threshold must be between {MinThreshold} and {MaxThreshold}.");
        }
    }
}
