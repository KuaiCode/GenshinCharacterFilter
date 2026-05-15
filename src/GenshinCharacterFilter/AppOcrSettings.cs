using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter;

/// <summary>
/// Stores OCR defaults loaded from local configuration.
/// </summary>
public sealed class AppOcrSettings
{
    public OcrEngine Engine { get; set; } = OcrEngine.TesseractCli;

    public string TesseractExecutablePath { get; set; } = OcrOptions.DefaultTesseractExecutablePath;

    public string Language { get; set; } = OcrOptions.DefaultLanguage;

    public int PageSegmentationMode { get; set; } = OcrOptions.DefaultPageSegmentationMode;

    public string? RegionConfigPath { get; set; }

    public string? RegionPreset { get; set; }

    public OcrRegion? Region { get; set; }

    /// <summary>
    /// Validates OCR settings before they are used by OCR commands.
    /// </summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Engine))
        {
            throw new AppSettingsException("Ocr.Engine is not supported.");
        }

        if (string.IsNullOrWhiteSpace(TesseractExecutablePath))
        {
            throw new AppSettingsException("Ocr.TesseractExecutablePath cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(Language))
        {
            throw new AppSettingsException("Ocr.Language cannot be empty.");
        }

        if (PageSegmentationMode is < OcrOptions.MinPageSegmentationMode or > OcrOptions.MaxPageSegmentationMode)
        {
            throw new AppSettingsException(
                $"Ocr.PageSegmentationMode must be between {OcrOptions.MinPageSegmentationMode} and {OcrOptions.MaxPageSegmentationMode}.");
        }

        try
        {
            Region?.ValidateShape();
            GetOcrRegionSourceOptions().Validate();
        }
        catch (ArgumentException exception)
        {
            throw new AppSettingsException(exception.Message, exception);
        }

        TesseractExecutablePath = TesseractExecutablePath.Trim();
        Language = Language.Trim();
        RegionConfigPath = string.IsNullOrWhiteSpace(RegionConfigPath) ? null : RegionConfigPath.Trim();
        RegionPreset = string.IsNullOrWhiteSpace(RegionPreset) ? null : RegionPreset.Trim();
    }

    /// <summary>
    /// Builds resolver options from configured OCR region source values.
    /// </summary>
    public OcrRegionSourceOptions GetOcrRegionSourceOptions()
    {
        return new OcrRegionSourceOptions
        {
            AbsoluteRegion = Region,
            CalibrationFilePath = RegionConfigPath,
            Preset = string.IsNullOrWhiteSpace(RegionPreset)
                ? null
                : OcrRegionPresetRegistry.Parse(RegionPreset)
        };
    }
}
