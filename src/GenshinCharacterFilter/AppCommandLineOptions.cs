using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Calibration;
using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter;

/// <summary>
/// Parses command-line options for the console application.
/// </summary>
public sealed class AppCommandLineOptions
{
    private bool _realAudioSpecified;
    private bool _targetProcessSpecified;
    private bool _audioModeSpecified;
    private bool _volumePercentSpecified;
    private bool _ocrLanguageSpecified;
    private bool _tesseractExecutablePathSpecified;
    private bool _ocrPageSegmentationModeSpecified;
    private bool _ocrRegionSpecified;
    private bool _ocrRegionConfigPathSpecified;
    private bool _ocrRegionPresetSpecified;
    private bool _loopIntervalMsSpecified;
    private bool _loopCountSpecified;
    private bool _matchThresholdSpecified;
    private bool _missThresholdSpecified;

    public string? ConfigPath { get; private init; }

    public bool UseRealAudio { get; private init; }

    public bool CaptureOnce { get; private init; }

    public bool OcrOnce { get; private init; }

    public bool DetectSpeakerOnce { get; private init; }

    public bool DetectLoop { get; private init; }

    public bool SimulateAudioFromDetection { get; private init; }

    public bool AllowRealAudioFromDetection { get; private init; }

    public bool CalibrateOcrRegion { get; private init; }

    public string TargetProcessName { get; private init; } = "GenshinImpact";

    public AudioFilterOptions AudioFilter { get; private init; } = new();

    public string CaptureOutputDirectory { get; private init; } = WindowCaptureOptions.DefaultOutputDirectory;

    public int CaptureDelayMs { get; private init; } = WindowCaptureOptions.DefaultCaptureDelayMs;

    public string? OcrInputPath { get; private init; }

    public string OcrLanguage { get; private init; } = OcrOptions.DefaultLanguage;

    public string TesseractExecutablePath { get; private init; } = OcrOptions.DefaultTesseractExecutablePath;

    public int OcrPageSegmentationMode { get; private init; } = OcrOptions.DefaultPageSegmentationMode;

    public OcrRegion? OcrRegion { get; private init; }

    public string? OcrRegionConfigPath { get; private init; }

    public OcrRegionPreset? OcrRegionPreset { get; private init; }

    public string? SpeakerText { get; private init; }

    public int LoopIntervalMs { get; private init; } = DetectionDryRunOptions.DefaultLoopIntervalMs;

    public int? LoopCount { get; private init; }

    public int MatchThreshold { get; private init; } = DetectionStabilityOptions.DefaultMatchThreshold;

    public int MissThreshold { get; private init; } = DetectionStabilityOptions.DefaultMissThreshold;

    public string CalibrationOutputPath { get; private init; } = OcrRegionCalibrationOptions.DefaultOutputPath;

    /// <summary>
    /// Parses command-line arguments into application options.
    /// </summary>
    public static AppCommandLineOptions Parse(string[] arguments)
    {
        string? audioMode = GetOptionValue(arguments, "--audio-mode");
        string? volumePercentValue = GetOptionValue(arguments, "--volume-percent");
        bool audioModeSpecified = audioMode is not null;
        bool volumePercentSpecified = volumePercentValue is not null;

        AudioFilterMode mode = audioModeSpecified
            ? ParseAudioMode(audioMode!)
            : AudioFilterMode.Mute;
        int volumePercent = AudioFilterOptions.DefaultVolumePercent;

        if (volumePercentSpecified &&
            !int.TryParse(volumePercentValue, out volumePercent))
        {
            throw new ArgumentException("Volume percent must be a number.", nameof(arguments));
        }

        AudioFilterOptions audioFilterOptions = new()
        {
            Mode = mode,
            VolumePercent = volumePercent
        };
        audioFilterOptions.Validate();

        string? processName = GetOptionValue(arguments, "--process");
        string? captureOutputDirectory = GetOptionValue(arguments, "--capture-output");
        string? captureDelayValue = GetOptionValue(arguments, "--capture-delay-ms");
        int captureDelayMs = WindowCaptureOptions.DefaultCaptureDelayMs;
        if (captureDelayValue is not null &&
            !int.TryParse(captureDelayValue, out captureDelayMs))
        {
            throw new ArgumentException("Capture delay ms must be a number.", nameof(arguments));
        }

        WindowCaptureOptions.ValidateCaptureDelayMs(captureDelayMs);

        bool ocrOnce = HasFlag(arguments, "--ocr-once");
        bool detectSpeakerOnce = HasFlag(arguments, "--detect-speaker-once");
        bool detectLoop = HasFlag(arguments, "--detect-loop");
        bool simulateAudioFromDetection = HasFlag(arguments, "--simulate-audio-from-detection");
        bool allowRealAudioFromDetection = HasFlag(arguments, "--allow-real-audio-from-detection");
        bool calibrateOcrRegion = HasFlag(arguments, "--calibrate-ocr-region");
        bool useRealAudio = HasFlag(arguments, "--real-audio");
        string? calibrationOutputPath = GetOptionValue(arguments, "--calibration-output");
        string? ocrInputPath = GetOptionValue(arguments, "--ocr-input");
        if (ocrOnce && ocrInputPath is null)
        {
            throw new ArgumentException("--ocr-once requires --ocr-input <imagePath>.", nameof(arguments));
        }

        string? speakerText = GetOptionValue(arguments, "--speaker-text");
        if (detectSpeakerOnce && speakerText is null && !ocrOnce)
        {
            throw new ArgumentException("--detect-speaker-once requires --speaker-text <text> or --ocr-once with --ocr-input <imagePath>.", nameof(arguments));
        }

        string? ocrLanguage = GetOptionValue(arguments, "--ocr-lang");
        string? tesseractExecutablePath = GetOptionValue(arguments, "--tesseract-path");
        string? ocrPsmValue = GetOptionValue(arguments, "--ocr-psm");
        string? ocrRegionValue = GetOptionValue(arguments, "--ocr-region");
        string? ocrRegionConfigPath = GetOptionValue(arguments, "--ocr-region-config");
        string? ocrRegionPresetValue = GetOptionValue(arguments, "--ocr-region-preset");
        bool ocrLanguageSpecified = ocrLanguage is not null;
        bool tesseractExecutablePathSpecified = tesseractExecutablePath is not null;
        bool ocrPageSegmentationModeSpecified = ocrPsmValue is not null;
        bool ocrRegionSpecified = ocrRegionValue is not null;
        bool ocrRegionConfigPathSpecified = ocrRegionConfigPath is not null;
        bool ocrRegionPresetSpecified = ocrRegionPresetValue is not null;
        int ocrPageSegmentationMode = OcrOptions.DefaultPageSegmentationMode;
        if (ocrPsmValue is not null &&
            !int.TryParse(ocrPsmValue, out ocrPageSegmentationMode))
        {
            throw new ArgumentException("OCR page segmentation mode must be a number.", nameof(arguments));
        }

        if (ocrPageSegmentationMode is < OcrOptions.MinPageSegmentationMode or > OcrOptions.MaxPageSegmentationMode)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arguments),
                $"OCR page segmentation mode must be between {OcrOptions.MinPageSegmentationMode} and {OcrOptions.MaxPageSegmentationMode}.");
        }

        OcrRegion? ocrRegion = ocrRegionValue is null
            ? null
            : GenshinCharacterFilter.Ocr.OcrRegion.Parse(ocrRegionValue);
        OcrRegionPreset? ocrRegionPreset = ocrRegionPresetValue is null
            ? null
            : OcrRegionPresetRegistry.Parse(ocrRegionPresetValue);
        OcrRegionSourceOptions ocrRegionSourceOptions = new()
        {
            AbsoluteRegion = ocrRegion,
            CalibrationFilePath = ocrRegionConfigPath,
            Preset = ocrRegionPreset
        };
        ocrRegionSourceOptions.Validate();

        string? loopIntervalValue = GetOptionValue(arguments, "--loop-interval-ms");
        bool loopIntervalMsSpecified = loopIntervalValue is not null;
        int loopIntervalMs = DetectionDryRunOptions.DefaultLoopIntervalMs;
        if (loopIntervalValue is not null &&
            !int.TryParse(loopIntervalValue, out loopIntervalMs))
        {
            throw new ArgumentException("Loop interval ms must be a number.", nameof(arguments));
        }

        DetectionDryRunOptions.ValidateLoopIntervalMs(loopIntervalMs);

        string? loopCountValue = GetOptionValue(arguments, "--loop-count");
        bool loopCountSpecified = loopCountValue is not null;
        int? loopCount = null;
        if (loopCountValue is not null)
        {
            if (!int.TryParse(loopCountValue, out int parsedLoopCount))
            {
                throw new ArgumentException("Loop count must be a number.", nameof(arguments));
            }

            loopCount = parsedLoopCount;
        }

        DetectionDryRunOptions.ValidateLoopCount(loopCount);

        bool matchThresholdSpecified = GetOptionValue(arguments, "--match-threshold") is not null;
        bool missThresholdSpecified = GetOptionValue(arguments, "--miss-threshold") is not null;
        int matchThreshold = ParseDetectionThreshold(
            arguments,
            "--match-threshold",
            DetectionStabilityOptions.DefaultMatchThreshold);
        int missThreshold = ParseDetectionThreshold(
            arguments,
            "--miss-threshold",
            DetectionStabilityOptions.DefaultMissThreshold);

        if (simulateAudioFromDetection && !detectLoop)
        {
            throw new ArgumentException("--simulate-audio-from-detection requires --detect-loop.", nameof(arguments));
        }

        if (simulateAudioFromDetection && HasFlag(arguments, "--real-audio"))
        {
            throw new ArgumentException("--simulate-audio-from-detection cannot be combined with --real-audio.", nameof(arguments));
        }

        if (allowRealAudioFromDetection && !useRealAudio)
        {
            throw new ArgumentException("--allow-real-audio-from-detection requires --real-audio.", nameof(arguments));
        }

        if (allowRealAudioFromDetection && !detectLoop)
        {
            throw new ArgumentException("--allow-real-audio-from-detection requires --detect-loop.", nameof(arguments));
        }

        if (detectLoop && useRealAudio && !allowRealAudioFromDetection)
        {
            throw new ArgumentException("--real-audio with --detect-loop requires --allow-real-audio-from-detection.", nameof(arguments));
        }

        if (calibrateOcrRegion && useRealAudio)
        {
            throw new ArgumentException("--calibrate-ocr-region cannot be combined with --real-audio.", nameof(arguments));
        }

        if (calibrateOcrRegion && processName is null)
        {
            throw new ArgumentException("--calibrate-ocr-region requires --process <name>.", nameof(arguments));
        }

        return new AppCommandLineOptions
        {
            ConfigPath = GetOptionValue(arguments, "--config"),
            UseRealAudio = useRealAudio,
            CaptureOnce = HasFlag(arguments, "--capture-once"),
            OcrOnce = ocrOnce,
            DetectSpeakerOnce = detectSpeakerOnce,
            DetectLoop = detectLoop,
            SimulateAudioFromDetection = simulateAudioFromDetection,
            AllowRealAudioFromDetection = allowRealAudioFromDetection,
            CalibrateOcrRegion = calibrateOcrRegion,
            TargetProcessName = processName ?? "GenshinImpact",
            AudioFilter = audioFilterOptions,
            CaptureOutputDirectory = captureOutputDirectory ?? WindowCaptureOptions.DefaultOutputDirectory,
            CaptureDelayMs = captureDelayMs,
            OcrInputPath = ocrInputPath,
            OcrLanguage = ocrLanguage ?? OcrOptions.DefaultLanguage,
            TesseractExecutablePath = tesseractExecutablePath ?? OcrOptions.DefaultTesseractExecutablePath,
            OcrPageSegmentationMode = ocrPageSegmentationMode,
            OcrRegion = ocrRegion,
            OcrRegionConfigPath = ocrRegionConfigPath,
            OcrRegionPreset = ocrRegionPreset,
            SpeakerText = speakerText,
            LoopIntervalMs = loopIntervalMs,
            LoopCount = loopCount,
            MatchThreshold = matchThreshold,
            MissThreshold = missThreshold,
            CalibrationOutputPath = calibrationOutputPath ?? OcrRegionCalibrationOptions.DefaultOutputPath,
            _realAudioSpecified = useRealAudio,
            _targetProcessSpecified = processName is not null,
            _audioModeSpecified = audioModeSpecified,
            _volumePercentSpecified = volumePercentSpecified,
            _ocrLanguageSpecified = ocrLanguageSpecified,
            _tesseractExecutablePathSpecified = tesseractExecutablePathSpecified,
            _ocrPageSegmentationModeSpecified = ocrPageSegmentationModeSpecified,
            _ocrRegionSpecified = ocrRegionSpecified,
            _ocrRegionConfigPathSpecified = ocrRegionConfigPathSpecified,
            _ocrRegionPresetSpecified = ocrRegionPresetSpecified,
            _loopIntervalMsSpecified = loopIntervalMsSpecified,
            _loopCountSpecified = loopCountSpecified,
            _matchThresholdSpecified = matchThresholdSpecified,
            _missThresholdSpecified = missThresholdSpecified
        };
    }

    /// <summary>
    /// Applies explicit CLI overrides to loaded settings.
    /// </summary>
    public AppSettings ApplyOverrides(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        bool ocrRegionSourceSpecified = _ocrRegionSpecified || _ocrRegionConfigPathSpecified || _ocrRegionPresetSpecified;

        // CLI only overrides explicitly supplied values; missing arguments preserve JSON or safe defaults.
        AppSettings merged = new()
        {
            TargetProcessName = _targetProcessSpecified ? TargetProcessName : settings.TargetProcessName,
            TargetSpeakers = [.. settings.TargetSpeakers],
            // v0.12 keeps real audio as CLI-only opt-in; config cannot enable runtime real audio by itself.
            RealAudioEnabled = _realAudioSpecified,
            AudioFilter = new AudioFilterOptions
            {
                Mode = _audioModeSpecified ? AudioFilter.Mode : settings.AudioFilter.Mode,
                VolumePercent = _volumePercentSpecified ? AudioFilter.VolumePercent : settings.AudioFilter.VolumePercent
            },
            Ocr = new AppOcrSettings
            {
                Engine = settings.Ocr.Engine,
                TesseractExecutablePath = _tesseractExecutablePathSpecified ? TesseractExecutablePath : settings.Ocr.TesseractExecutablePath,
                Language = _ocrLanguageSpecified ? OcrLanguage : settings.Ocr.Language,
                PageSegmentationMode = _ocrPageSegmentationModeSpecified ? OcrPageSegmentationMode : settings.Ocr.PageSegmentationMode,
                Region = ocrRegionSourceSpecified ? (_ocrRegionSpecified ? OcrRegion : null) : settings.Ocr.Region,
                RegionConfigPath = ocrRegionSourceSpecified ? (_ocrRegionConfigPathSpecified ? OcrRegionConfigPath : null) : settings.Ocr.RegionConfigPath,
                RegionPreset = ocrRegionSourceSpecified ? (_ocrRegionPresetSpecified ? ToPresetConfigValue(OcrRegionPreset) : null) : settings.Ocr.RegionPreset
            },
            Detection = new AppDetectionSettings
            {
                LoopIntervalMs = _loopIntervalMsSpecified ? LoopIntervalMs : settings.Detection.LoopIntervalMs,
                LoopCount = _loopCountSpecified ? LoopCount : settings.Detection.LoopCount,
                MatchThreshold = _matchThresholdSpecified ? MatchThreshold : settings.Detection.MatchThreshold,
                MissThreshold = _missThresholdSpecified ? MissThreshold : settings.Detection.MissThreshold
            }
        };

        merged.Validate();
        ValidateMergedCommandSafety(merged);
        return merged;
    }

    private static AudioFilterMode ParseAudioMode(string audioMode)
    {
        return audioMode.Trim().ToLowerInvariant() switch
        {
            "mute" => AudioFilterMode.Mute,
            "reduce" => AudioFilterMode.ReduceVolume,
            "reduce-volume" => AudioFilterMode.ReduceVolume,
            _ => throw new ArgumentException("Audio mode must be 'mute' or 'reduce'.", nameof(audioMode))
        };
    }

    /// <summary>
    /// Builds OCR region source options from parsed CLI values.
    /// </summary>
    public OcrRegionSourceOptions GetOcrRegionSourceOptions()
    {
        return new OcrRegionSourceOptions
        {
            AbsoluteRegion = OcrRegion,
            CalibrationFilePath = OcrRegionConfigPath,
            Preset = OcrRegionPreset
        };
    }

    private void ValidateMergedCommandSafety(AppSettings merged)
    {
        if (!DetectLoop)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(OcrInputPath) &&
            string.IsNullOrWhiteSpace(merged.TargetProcessName))
        {
            throw new ArgumentException("--detect-loop requires --ocr-input <imagePath> or configured TargetProcessName.");
        }

        if (AllowRealAudioFromDetection &&
            !merged.Ocr.GetOcrRegionSourceOptions().HasEffectiveRegionSource)
        {
            throw new ArgumentException("--allow-real-audio-from-detection requires --ocr-region, --ocr-region-config, --ocr-region-preset, or an OCR region source in config.");
        }
    }

    private static string? ToPresetConfigValue(OcrRegionPreset? preset)
    {
        return preset is null
            ? null
            : OcrRegionPresetRegistry.GetDisplayName(preset.Value);
    }

    private static int ParseDetectionThreshold(string[] arguments, string optionName, int defaultValue)
    {
        string? value = GetOptionValue(arguments, optionName);
        if (value is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out int threshold))
        {
            throw new ArgumentException($"{optionName} must be a number.", nameof(arguments));
        }

        DetectionStabilityOptions.ValidateThreshold(threshold, optionName);
        return threshold;
    }

    private static string? GetOptionValue(string[] arguments, string optionName)
    {
        for (int i = 0; i < arguments.Length; i++)
        {
            if (!string.Equals(arguments[i], optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i == arguments.Length - 1 ||
                IsOptionName(arguments[i + 1]) ||
                string.IsNullOrWhiteSpace(arguments[i + 1]))
            {
                throw new ArgumentException($"{optionName} requires a value.", nameof(arguments));
            }

            return arguments[i + 1];
        }

        return null;
    }

    private static bool HasFlag(string[] arguments, string optionName)
    {
        return arguments.Any(argument => string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOptionName(string value)
    {
        return value.StartsWith("--", StringComparison.Ordinal);
    }
}
