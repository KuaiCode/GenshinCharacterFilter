using GenshinCharacterFilter.Detection;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// UI-independent projection of one detection iteration.
/// </summary>
public readonly record struct GuiLastObservation(
    string OcrText,
    string? StableSpeaker,
    string? RawSpeaker,
    DetectionAudioAction AudioAction);
