using GenshinCharacterFilter.Audio;
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

    public string? ConfigPath { get; private init; }

    public bool UseRealAudio { get; private init; }

    public bool CaptureOnce { get; private init; }

    public bool OcrOnce { get; private init; }

    public bool DetectSpeakerOnce { get; private init; }

    public bool DetectLoop { get; private init; }

    public bool SimulateAudioFromDetection { get; private init; }

    public string TargetProcessName { get; private init; } = "GenshinImpact";

    public AudioFilterOptions AudioFilter { get; private init; } = new();

    public string CaptureOutputDirectory { get; private init; } = WindowCaptureOptions.DefaultOutputDirectory;

    public int CaptureDelayMs { get; private init; } = WindowCaptureOptions.DefaultCaptureDelayMs;

    public string? OcrInputPath { get; private init; }

    public string OcrLanguage { get; private init; } = OcrOptions.DefaultLanguage;

    public string TesseractExecutablePath { get; private init; } = OcrOptions.DefaultTesseractExecutablePath;

    public int OcrPageSegmentationMode { get; private init; } = OcrOptions.DefaultPageSegmentationMode;

    public OcrRegion? OcrRegion { get; private init; }

    public string? SpeakerText { get; private init; }

    public int LoopIntervalMs { get; private init; } = DetectionDryRunOptions.DefaultLoopIntervalMs;

    public int? LoopCount { get; private init; }

    public int MatchThreshold { get; private init; } = DetectionStabilityOptions.DefaultMatchThreshold;

    public int MissThreshold { get; private init; } = DetectionStabilityOptions.DefaultMissThreshold;

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

        string? loopIntervalValue = GetOptionValue(arguments, "--loop-interval-ms");
        int loopIntervalMs = DetectionDryRunOptions.DefaultLoopIntervalMs;
        if (loopIntervalValue is not null &&
            !int.TryParse(loopIntervalValue, out loopIntervalMs))
        {
            throw new ArgumentException("Loop interval ms must be a number.", nameof(arguments));
        }

        DetectionDryRunOptions.ValidateLoopIntervalMs(loopIntervalMs);

        string? loopCountValue = GetOptionValue(arguments, "--loop-count");
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

        int matchThreshold = ParseDetectionThreshold(
            arguments,
            "--match-threshold",
            DetectionStabilityOptions.DefaultMatchThreshold);
        int missThreshold = ParseDetectionThreshold(
            arguments,
            "--miss-threshold",
            DetectionStabilityOptions.DefaultMissThreshold);

        if (detectLoop && ocrInputPath is null && processName is null)
        {
            throw new ArgumentException("--detect-loop requires --ocr-input <imagePath> or --process <name>.", nameof(arguments));
        }

        if (detectLoop && ocrInputPath is null && processName is not null && ocrRegion is null)
        {
            throw new ArgumentException("--detect-loop process mode requires --ocr-region <x,y,width,height>.", nameof(arguments));
        }

        if (simulateAudioFromDetection && !detectLoop)
        {
            throw new ArgumentException("--simulate-audio-from-detection requires --detect-loop.", nameof(arguments));
        }

        if (simulateAudioFromDetection && HasFlag(arguments, "--real-audio"))
        {
            throw new ArgumentException("--simulate-audio-from-detection cannot be combined with --real-audio.", nameof(arguments));
        }

        return new AppCommandLineOptions
        {
            ConfigPath = GetOptionValue(arguments, "--config"),
            UseRealAudio = HasFlag(arguments, "--real-audio"),
            CaptureOnce = HasFlag(arguments, "--capture-once"),
            OcrOnce = ocrOnce,
            DetectSpeakerOnce = detectSpeakerOnce,
            DetectLoop = detectLoop,
            SimulateAudioFromDetection = simulateAudioFromDetection,
            TargetProcessName = processName ?? "GenshinImpact",
            AudioFilter = audioFilterOptions,
            CaptureOutputDirectory = captureOutputDirectory ?? WindowCaptureOptions.DefaultOutputDirectory,
            CaptureDelayMs = captureDelayMs,
            OcrInputPath = ocrInputPath,
            OcrLanguage = ocrLanguage ?? OcrOptions.DefaultLanguage,
            TesseractExecutablePath = tesseractExecutablePath ?? OcrOptions.DefaultTesseractExecutablePath,
            OcrPageSegmentationMode = ocrPageSegmentationMode,
            OcrRegion = ocrRegion,
            SpeakerText = speakerText,
            LoopIntervalMs = loopIntervalMs,
            LoopCount = loopCount,
            MatchThreshold = matchThreshold,
            MissThreshold = missThreshold,
            _realAudioSpecified = HasFlag(arguments, "--real-audio"),
            _targetProcessSpecified = processName is not null,
            _audioModeSpecified = audioModeSpecified,
            _volumePercentSpecified = volumePercentSpecified
        };
    }

    /// <summary>
    /// Applies explicit CLI overrides to loaded settings.
    /// </summary>
    public AppSettings ApplyOverrides(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // CLI only overrides explicitly supplied values; missing arguments preserve JSON or safe defaults.
        AppSettings merged = new()
        {
            TargetProcessName = _targetProcessSpecified ? TargetProcessName : settings.TargetProcessName,
            TargetSpeakers = [.. settings.TargetSpeakers],
            RealAudioEnabled = _realAudioSpecified || settings.RealAudioEnabled,
            AudioFilter = new AudioFilterOptions
            {
                Mode = _audioModeSpecified ? AudioFilter.Mode : settings.AudioFilter.Mode,
                VolumePercent = _volumePercentSpecified ? AudioFilter.VolumePercent : settings.AudioFilter.VolumePercent
            }
        };

        merged.Validate();
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
