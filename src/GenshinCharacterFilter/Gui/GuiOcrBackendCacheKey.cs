using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Identifies the runtime state that makes an OCR backend instance reusable.
/// </summary>
public sealed record GuiOcrBackendCacheKey(
    OcrEngine Engine,
    string? PaddleModelDirectory,
    string? PaddleRuntimeDirectory)
{
    public static GuiOcrBackendCacheKey From(
        OcrEngine engine,
        string? paddleModelDirectory,
        string? paddleRuntimeDirectory)
    {
        return engine == OcrEngine.PaddleOcrLocal
            ? new GuiOcrBackendCacheKey(
                engine,
                NormalizePath(paddleModelDirectory),
                NormalizePath(paddleRuntimeDirectory))
            : new GuiOcrBackendCacheKey(engine, null, null);
    }

    private static string? NormalizePath(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : Path.GetFullPath(path.Trim());
    }
}
