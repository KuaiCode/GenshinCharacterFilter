using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Gui;

public sealed record GuiOcrWarmupResult(
    OcrEngine Engine,
    bool IsWarm,
    long ElapsedMs);
