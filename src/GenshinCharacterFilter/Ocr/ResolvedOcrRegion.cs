namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Contains the OCR region resolved for a specific input image.
/// </summary>
public sealed record ResolvedOcrRegion(OcrRegion? Region, string SourceLabel)
{
    public static ResolvedOcrRegion FullImage { get; } = new(null, "none/full image");
}
