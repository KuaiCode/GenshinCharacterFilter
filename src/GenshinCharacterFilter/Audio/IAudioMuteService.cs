namespace GenshinCharacterFilter.Audio;

/// <summary>
/// Controls mute and restore requests for the target audio session.
/// </summary>
public interface IAudioMuteService
{
    /// <summary>
    /// Requests muting the target audio session.
    /// </summary>
    Task MuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Requests restoring the target audio session.
    /// </summary>
    Task RestoreAsync(CancellationToken cancellationToken);
}
