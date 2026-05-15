namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Describes explicit OCR region source choices.
/// </summary>
public sealed class OcrRegionSourceOptions
{
    public OcrRegion? AbsoluteRegion { get; init; }

    public string? CalibrationFilePath { get; init; }

    public OcrRegionPreset? Preset { get; init; }

    /// <summary>
    /// True when a source can produce a specific OCR region.
    /// </summary>
    public bool HasEffectiveRegionSource =>
        AbsoluteRegion is not null ||
        !string.IsNullOrWhiteSpace(CalibrationFilePath) ||
        (Preset is not null && Preset != OcrRegionPreset.None);

    /// <summary>
    /// Validates mutually exclusive region source choices.
    /// </summary>
    public void Validate()
    {
        AbsoluteRegion?.ValidateShape();

        bool hasCalibrationFile = !string.IsNullOrWhiteSpace(CalibrationFilePath);
        bool hasExplicitPreset = Preset is not null && Preset != OcrRegionPreset.None;

        if (Preset is not null && !Enum.IsDefined(Preset.Value))
        {
            throw new ArgumentException("OCR region preset is not supported.", nameof(Preset));
        }

        if (AbsoluteRegion is not null && hasCalibrationFile)
        {
            throw new ArgumentException("--ocr-region and --ocr-region-config cannot be used together.");
        }

        if (AbsoluteRegion is not null && hasExplicitPreset)
        {
            throw new ArgumentException("--ocr-region and explicit --ocr-region-preset cannot be used together unless preset is none.");
        }

        if (hasCalibrationFile && hasExplicitPreset)
        {
            throw new ArgumentException("--ocr-region-config and explicit --ocr-region-preset cannot be used together unless preset is none.");
        }
    }
}
