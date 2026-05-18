using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter;

/// <summary>
/// Prints a concise summary of the merged runtime configuration.
/// </summary>
public sealed class EffectiveConfigPrinter
{
    /// <summary>
    /// Writes the effective settings and safety-gate state without starting runtime work.
    /// </summary>
    public void Print(AppSettings settings, AppCommandLineOptions options, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(writer);

        bool detectionRealAudioAllowed =
            options.DetectLoop &&
            settings.RealAudioEnabled &&
            options.AllowRealAudioFromDetection;

        writer.WriteLine("Effective configuration:");
        writer.WriteLine($"TargetProcessName: {settings.TargetProcessName}");
        writer.WriteLine($"TargetSpeakers: {string.Join(", ", settings.TargetSpeakers)}");
        writer.WriteLine($"RealAudioEnabled: {settings.RealAudioEnabled}");
        writer.WriteLine($"AllowRealAudioFromDetection: {options.AllowRealAudioFromDetection}");
        writer.WriteLine($"Detection real audio allowed: {detectionRealAudioAllowed}");
        writer.WriteLine($"AudioFilter.Mode: {settings.AudioFilter.Mode}");
        writer.WriteLine($"AudioFilter.VolumePercent: {settings.AudioFilter.VolumePercent}");
        writer.WriteLine($"Ocr.Engine: {settings.Ocr.Engine}");
        writer.WriteLine($"Ocr.TesseractExecutablePath: {settings.Ocr.TesseractExecutablePath}");
        writer.WriteLine($"Ocr.Language: {settings.Ocr.Language}");
        writer.WriteLine($"Ocr.PageSegmentationMode: {settings.Ocr.PageSegmentationMode}");
        writer.WriteLine($"Ocr.RegionSource: {DescribeRegionSource(settings.Ocr.GetOcrRegionSourceOptions())}");
        writer.WriteLine($"Detection.LoopIntervalMs: {settings.Detection.LoopIntervalMs}");
        writer.WriteLine($"Detection.LoopCount: {settings.Detection.LoopCount?.ToString() ?? "(until Ctrl+C)"}");
        writer.WriteLine($"Detection.MatchThreshold: {settings.Detection.MatchThreshold}");
        writer.WriteLine($"Detection.MissThreshold: {settings.Detection.MissThreshold}");
        writer.WriteLine($"Detection.SaveDebugImages: {settings.Detection.SaveDebugImages}");
    }

    private static string DescribeRegionSource(OcrRegionSourceOptions options)
    {
        if (options.AbsoluteRegion is not null)
        {
            return $"absolute {options.AbsoluteRegion}";
        }

        if (!string.IsNullOrWhiteSpace(options.CalibrationFilePath))
        {
            return $"calibration file {options.CalibrationFilePath}";
        }

        if (options.Preset is not null)
        {
            return $"preset {OcrRegionPresetRegistry.GetDisplayName(options.Preset.Value)}";
        }

        return "none/full image";
    }
}
