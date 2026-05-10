namespace GenshinCharacterFilter.Coordination;

/// <summary>
/// Represents the coordinator's mute lifecycle state.
/// </summary>
public enum MuteCoordinatorState
{
    Idle,
    Muted,
    Restoring,
    Faulted
}
