namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Matches raw OCR or manual text against configured target speaker names.
/// </summary>
public interface ISpeakerMatcher
{
    /// <summary>
    /// Normalizes text and returns whether it matches a configured target speaker.
    /// </summary>
    SpeakerMatchResult Match(string? rawText, SpeakerMatcherOptions options);
}
