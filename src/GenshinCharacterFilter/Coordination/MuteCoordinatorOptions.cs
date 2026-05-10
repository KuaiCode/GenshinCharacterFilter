namespace GenshinCharacterFilter.Coordination;

/// <summary>
/// Options used by <see cref="MuteCoordinator"/>.
/// </summary>
public sealed class MuteCoordinatorOptions
{
    /// <summary>
    /// Gets the speaker names that should trigger mute coordination.
    /// </summary>
    public IReadOnlySet<string> TargetSpeakers { get; init; } = new HashSet<string>();

    /// <summary>
    /// Gets whether speaker matching should use case-sensitive comparison.
    /// </summary>
    public bool CaseSensitiveSpeakerMatching { get; init; }
}
