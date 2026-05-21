namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Optional capability for OCR backends that can initialize expensive runtime state before the first OCR call.
/// </summary>
public interface IOcrBackendWarmup
{
    bool IsWarm { get; }

    Task WarmUpAsync(OcrOptions options, CancellationToken cancellationToken);
}
