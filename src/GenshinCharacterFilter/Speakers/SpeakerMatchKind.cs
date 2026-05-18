namespace GenshinCharacterFilter.Speakers;

/// <summary>
/// Describes how strongly OCR text matched a target speaker.
/// </summary>
public enum SpeakerMatchKind
{
    None,
    Strong,
    Weak,
    Unknown
}
