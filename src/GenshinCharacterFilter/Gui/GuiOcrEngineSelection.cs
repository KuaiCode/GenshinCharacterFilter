using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Gui;

public static class GuiOcrEngineSelection
{
    public static OcrEngine Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return OcrEngine.TesseractCli;
        }

        foreach (OcrEngine engine in Enum.GetValues<OcrEngine>())
        {
            if (string.Equals(value.Trim(), engine.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return engine;
            }
        }

        throw new ArgumentException("OCR engine must be TesseractCli or PaddleOcrLocal.", nameof(value));
    }
}
