namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Options for matching OCR or manual text against target speakers.
/// </summary>
public sealed class SpeakerMatcherOptions
{
    /// <summary>
    /// Gets the configured target speaker names.
    /// </summary>
    public IReadOnlyCollection<string> TargetSpeakers { get; init; } = [];

    /// <summary>
    /// Gets whether speaker matching should be case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; init; }
}
