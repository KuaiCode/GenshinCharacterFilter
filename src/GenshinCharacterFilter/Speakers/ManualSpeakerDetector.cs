namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Provides the latest manually entered speaker name for simulated detection.
/// </summary>
public sealed class ManualSpeakerDetector : ISpeakerDetector
{
    private string? _currentSpeaker;

    /// <summary>
    /// Sets the current simulated speaker name.
    /// </summary>
    public void SetSpeaker(string? speaker)
    {
        _currentSpeaker = string.IsNullOrWhiteSpace(speaker)
            ? null
            : speaker.Trim();
    }

    /// <inheritdoc />
    public Task<string?> DetectSpeakerAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_currentSpeaker);
    }
}
