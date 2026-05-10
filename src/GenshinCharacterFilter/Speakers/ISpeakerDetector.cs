namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Detects the current speaker from simulated or future pluggable input.
/// </summary>
public interface ISpeakerDetector
{
    /// <summary>
    /// Detects the current speaker name, or returns null when unknown.
    /// </summary>
    Task<string?> DetectSpeakerAsync(CancellationToken cancellationToken);
}
