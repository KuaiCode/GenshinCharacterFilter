namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Creates OCR services for the configured backend.
/// </summary>
public static class OcrServiceFactory
{
    public static IOcrService Create(OcrEngine engine)
    {
        return engine switch
        {
            OcrEngine.TesseractCli => new TesseractCliOcrService(),
            OcrEngine.PaddleOcrLocal => new PaddleOcrLocalService(),
            _ => throw new OcrException($"OCR engine '{engine}' is not supported.")
        };
    }
}
