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
    private bool _ocrEngineSpecified;
    private bool _ocrLanguageSpecified;
    private bool _tesseractExecutablePathSpecified;
    private bool _paddleModelDirectorySpecified;
    private bool _paddleRuntimeDirectorySpecified;
    private bool _ocrPageSegmentationModeSpecified;
    private bool _ocrInputScaleSpecified;
    private bool _ocrPaddingPixelsSpecified;
    private bool _ocrGrayscaleSpecified;
    private bool _ocrInvertSpecified;
    private bool _ocrThresholdSpecified;
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

    public bool OcrBenchmark { get; private init; }

    public bool DetectSpeakerOnce { get; private init; }

    public bool DetectLoop { get; private init; }

    public bool SimulateAudioFromDetection { get; private init; }

    public bool AllowRealAudioFromDetection { get; private init; }

    public bool CalibrateOcrRegion { get; private init; }

    public bool ValidateConfig { get; private init; }

    public bool PrintEffectiveConfig { get; private init; }

    public bool Gui { get; private init; }

    public string TargetProcessName { get; private init; } = "GenshinImpact";

    public AudioFilterOptions AudioFilter { get; private init; } = new();

    public string CaptureOutputDirectory { get; private init; } = WindowCaptureOptions.DefaultOutputDirectory;

    public int CaptureDelayMs { get; private init; } = WindowCaptureOptions.DefaultCaptureDelayMs;

    public string? OcrInputPath { get; private init; }

    public string OcrLanguage { get; private init; } = OcrOptions.DefaultLanguage;

    public OcrEngine OcrEngine { get; private init; } = OcrEngine.TesseractCli;

    public string TesseractExecutablePath { get; private init; } = OcrOptions.DefaultTesseractExecutablePath;

    public string? PaddleModelDirectory { get; private init; }

    public string? PaddleRuntimeDirectory { get; private init; }

    public int OcrPageSegmentationMode { get; private init; } = OcrOptions.DefaultPageSegmentationMode;

    public int OcrInputScale { get; private init; } = OcrOptions.DefaultInputScale;

    public int OcrPaddingPixels { get; private init; } = OcrOptions.DefaultPaddingPixels;

    public bool OcrGrayscale { get; private init; }

    public bool OcrInvert { get; private init; }

    public int? OcrThreshold { get; private init; }

    public OcrRegion? OcrRegion { get; private init; }

    public string? OcrRegionConfigPath { get; private init; }

    public OcrRegionPreset? OcrRegionPreset { get; private init; }

    public string? SpeakerText { get; private init; }

    public int LoopIntervalMs { get; private init; } = DetectionDryRunOptions.DefaultLoopIntervalMs;

    public int? LoopCount { get; private init; }

    public int MatchThreshold { get; private init; } = DetectionStabilityOptions.DefaultMatchThreshold;

    public int MissThreshold { get; private init; } = DetectionStabilityOptions.DefaultMissThreshold;

    public int OcrRepeat { get; private init; } = 1;

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
        bool ocrBenchmark = HasFlag(arguments, "--ocr-benchmark");
        bool detectSpeakerOnce = HasFlag(arguments, "--detect-speaker-once");
        bool detectLoop = HasFlag(arguments, "--detect-loop");
        bool simulateAudioFromDetection = HasFlag(arguments, "--simulate-audio-from-detection");
        bool allowRealAudioFromDetection = HasFlag(arguments, "--allow-real-audio-from-detection");
        bool calibrateOcrRegion = HasFlag(arguments, "--calibrate-ocr-region");
        bool validateConfig = HasFlag(arguments, "--validate-config");
        bool printEffectiveConfig = HasFlag(arguments, "--print-effective-config");
        bool gui = HasFlag(arguments, "--gui");
        bool useRealAudio = HasFlag(arguments, "--real-audio");
        string? calibrationOutputPath = GetOptionValue(arguments, "--calibration-output");
        string? ocrInputPath = GetOptionValue(arguments, "--ocr-input");
        if ((ocrOnce || ocrBenchmark) && ocrInputPath is null)
        {
            throw new ArgumentException("--ocr-once and --ocr-benchmark require --ocr-input <imagePath>.", nameof(arguments));
        }

        string? speakerText = GetOptionValue(arguments, "--speaker-text");
        if (detectSpeakerOnce && speakerText is null && !ocrOnce)
        {
            throw new ArgumentException("--detect-speaker-once requires --speaker-text <text> or --ocr-once with --ocr-input <imagePath>.", nameof(arguments));
        }

        string? ocrEngineValue = GetOptionValue(arguments, "--ocr-engine");
        string? ocrLanguage = GetOptionValue(arguments, "--ocr-lang");
        string? tesseractExecutablePath = GetOptionValue(arguments, "--tesseract-path");
        string? paddleModelDirectory = GetOptionValue(arguments, "--paddle-model-dir");
        string? paddleRuntimeDirectory = GetOptionValue(arguments, "--paddle-runtime-dir");
        string? ocrPsmValue = GetOptionValue(arguments, "--ocr-psm");
        string? ocrInputScaleValue = GetOptionValue(arguments, "--ocr-input-scale");
        string? ocrPaddingPixelsValue = GetOptionValue(arguments, "--ocr-padding-pixels");
        bool ocrGrayscale = HasFlag(arguments, "--ocr-grayscale");
        bool ocrInvert = HasFlag(arguments, "--ocr-invert");
        string? ocrThresholdValue = GetOptionValue(arguments, "--ocr-threshold");
        string? ocrRegionValue = GetOptionValue(arguments, "--ocr-region");
        string? ocrRegionConfigPath = GetOptionValue(arguments, "--ocr-region-config");
        string? ocrRegionPresetValue = GetOptionValue(arguments, "--ocr-region-preset");
        string? ocrRepeatValue = GetOptionValue(arguments, "--ocr-repeat");
        bool ocrEngineSpecified = ocrEngineValue is not null;
        bool ocrLanguageSpecified = ocrLanguage is not null;
        bool tesseractExecutablePathSpecified = tesseractExecutablePath is not null;
        bool paddleModelDirectorySpecified = paddleModelDirectory is not null;
        bool paddleRuntimeDirectorySpecified = paddleRuntimeDirectory is not null;
        bool ocrPageSegmentationModeSpecified = ocrPsmValue is not null;
        bool ocrInputScaleSpecified = ocrInputScaleValue is not null;
        bool ocrPaddingPixelsSpecified = ocrPaddingPixelsValue is not null;
        bool ocrGrayscaleSpecified = ocrGrayscale;
        bool ocrInvertSpecified = ocrInvert;
        bool ocrThresholdSpecified = ocrThresholdValue is not null;
        bool ocrRegionSpecified = ocrRegionValue is not null;
        bool ocrRegionConfigPathSpecified = ocrRegionConfigPath is not null;
        bool ocrRegionPresetSpecified = ocrRegionPresetValue is not null;
        OcrEngine ocrEngine = ocrEngineValue is null
            ? OcrEngine.TesseractCli
            : ParseOcrEngine(ocrEngineValue);

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

        int ocrInputScale = ParseOptionalInt(
            ocrInputScaleValue,
            OcrOptions.DefaultInputScale,
            "OCR input scale must be a number.");
        int ocrPaddingPixels = ParseOptionalInt(
            ocrPaddingPixelsValue,
            OcrOptions.DefaultPaddingPixels,
            "OCR padding pixels must be a number.");
        int? ocrThreshold = ParseOptionalThresholdValue(ocrThresholdValue);
        OcrOptions.ValidatePreparationOptions(ocrInputScale, ocrPaddingPixels, ocrThreshold);

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

        int ocrRepeat = 1;
        if (ocrRepeatValue is not null)
        {
            if (!int.TryParse(ocrRepeatValue, out ocrRepeat))
            {
                throw new ArgumentException("OCR repeat must be a number.", nameof(arguments));
            }

            if (ocrRepeat <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arguments), "OCR repeat must be greater than 0.");
            }
        }

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
            OcrBenchmark = ocrBenchmark,
            DetectSpeakerOnce = detectSpeakerOnce,
            DetectLoop = detectLoop,
            SimulateAudioFromDetection = simulateAudioFromDetection,
            AllowRealAudioFromDetection = allowRealAudioFromDetection,
            CalibrateOcrRegion = calibrateOcrRegion,
            ValidateConfig = validateConfig,
            PrintEffectiveConfig = printEffectiveConfig,
            Gui = gui,
            TargetProcessName = processName ?? "GenshinImpact",
            AudioFilter = audioFilterOptions,
            CaptureOutputDirectory = captureOutputDirectory ?? WindowCaptureOptions.DefaultOutputDirectory,
            CaptureDelayMs = captureDelayMs,
            OcrInputPath = ocrInputPath,
            OcrLanguage = ocrLanguage ?? OcrOptions.DefaultLanguage,
            OcrEngine = ocrEngine,
            TesseractExecutablePath = tesseractExecutablePath ?? OcrOptions.DefaultTesseractExecutablePath,
            PaddleModelDirectory = paddleModelDirectory,
            PaddleRuntimeDirectory = paddleRuntimeDirectory,
            OcrPageSegmentationMode = ocrPageSegmentationMode,
            OcrInputScale = ocrInputScale,
            OcrPaddingPixels = ocrPaddingPixels,
            OcrGrayscale = ocrGrayscale,
            OcrInvert = ocrInvert,
            OcrThreshold = ocrThreshold,
            OcrRegion = ocrRegion,
            OcrRegionConfigPath = ocrRegionConfigPath,
            OcrRegionPreset = ocrRegionPreset,
            SpeakerText = speakerText,
            LoopIntervalMs = loopIntervalMs,
            LoopCount = loopCount,
            MatchThreshold = matchThreshold,
            MissThreshold = missThreshold,
            OcrRepeat = ocrRepeat,
            CalibrationOutputPath = calibrationOutputPath ?? OcrRegionCalibrationOptions.DefaultOutputPath,
            _realAudioSpecified = useRealAudio,
            _targetProcessSpecified = processName is not null,
            _audioModeSpecified = audioModeSpecified,
            _volumePercentSpecified = volumePercentSpecified,
            _ocrEngineSpecified = ocrEngineSpecified,
            _ocrLanguageSpecified = ocrLanguageSpecified,
            _tesseractExecutablePathSpecified = tesseractExecutablePathSpecified,
            _paddleModelDirectorySpecified = paddleModelDirectorySpecified,
            _paddleRuntimeDirectorySpecified = paddleRuntimeDirectorySpecified,
            _ocrPageSegmentationModeSpecified = ocrPageSegmentationModeSpecified,
            _ocrInputScaleSpecified = ocrInputScaleSpecified,
            _ocrPaddingPixelsSpecified = ocrPaddingPixelsSpecified,
            _ocrGrayscaleSpecified = ocrGrayscaleSpecified,
            _ocrInvertSpecified = ocrInvertSpecified,
            _ocrThresholdSpecified = ocrThresholdSpecified,
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
                Engine = _ocrEngineSpecified ? OcrEngine : settings.Ocr.Engine,
                TesseractExecutablePath = _tesseractExecutablePathSpecified ? TesseractExecutablePath : settings.Ocr.TesseractExecutablePath,
                PaddleModelDirectory = _paddleModelDirectorySpecified ? PaddleModelDirectory : settings.Ocr.PaddleModelDirectory,
                PaddleRuntimeDirectory = _paddleRuntimeDirectorySpecified ? PaddleRuntimeDirectory : settings.Ocr.PaddleRuntimeDirectory,
                Language = _ocrLanguageSpecified ? OcrLanguage : settings.Ocr.Language,
                PageSegmentationMode = _ocrPageSegmentationModeSpecified ? OcrPageSegmentationMode : settings.Ocr.PageSegmentationMode,
                InputScale = _ocrInputScaleSpecified ? OcrInputScale : settings.Ocr.InputScale,
                PaddingPixels = _ocrPaddingPixelsSpecified ? OcrPaddingPixels : settings.Ocr.PaddingPixels,
                Grayscale = _ocrGrayscaleSpecified ? OcrGrayscale : settings.Ocr.Grayscale,
                Invert = _ocrInvertSpecified ? OcrInvert : settings.Ocr.Invert,
                Threshold = _ocrThresholdSpecified ? OcrThreshold : settings.Ocr.Threshold,
                Region = ocrRegionSourceSpecified ? (_ocrRegionSpecified ? OcrRegion : null) : settings.Ocr.Region,
                RegionConfigPath = ocrRegionSourceSpecified ? (_ocrRegionConfigPathSpecified ? OcrRegionConfigPath : null) : settings.Ocr.RegionConfigPath,
                RegionPreset = ocrRegionSourceSpecified ? (_ocrRegionPresetSpecified ? ToPresetConfigValue(OcrRegionPreset) : null) : settings.Ocr.RegionPreset
            },
            Detection = new AppDetectionSettings
            {
                LoopIntervalMs = _loopIntervalMsSpecified ? LoopIntervalMs : settings.Detection.LoopIntervalMs,
                LoopCount = _loopCountSpecified ? LoopCount : settings.Detection.LoopCount,
                MatchThreshold = _matchThresholdSpecified ? MatchThreshold : settings.Detection.MatchThreshold,
                MissThreshold = _missThresholdSpecified ? MissThreshold : settings.Detection.MissThreshold,
                SaveDebugImages = settings.Detection.SaveDebugImages,
                SaveOcrFailureSamples = settings.Detection.SaveOcrFailureSamples
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

    private static OcrEngine ParseOcrEngine(string value)
    {
        string normalized = value.Trim();
        foreach (OcrEngine engine in Enum.GetValues<OcrEngine>())
        {
            if (string.Equals(normalized, engine.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return engine;
            }
        }

        throw new ArgumentException("OCR engine must be 'TesseractCli' or 'PaddleOcrLocal'.", nameof(value));
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

    private static int ParseOptionalInt(string? value, int defaultValue, string errorMessage)
    {
        if (value is null)
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out int parsed))
        {
            throw new ArgumentException(errorMessage, nameof(value));
        }

        return parsed;
    }

    private static int? ParseOptionalThresholdValue(string? value)
    {
        if (value is null ||
            string.Equals(value.Trim(), "none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!int.TryParse(value, out int threshold))
        {
            throw new ArgumentException("OCR threshold must be a number or 'none'.", nameof(value));
        }

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
