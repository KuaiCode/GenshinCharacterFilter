using GenshinCharacterFilter.Capture;
using GenshinCharacterFilter.Detection;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Runtime-only GUI overrides for detection timing and stability settings.
/// </summary>
public sealed class GuiDetectionTuningOptions
{
    public const int DefaultLoopIntervalMs = 200;
    public const int DefaultCaptureDelayMs = 100;
    public const int DefaultMatchThreshold = 2;
    public const int DefaultMissThreshold = 1;

    public bool RunUntilStop { get; init; }

    public int? LoopCount { get; init; }

    public int? LoopIntervalMs { get; init; }

    public int? CaptureDelayMs { get; init; }

    public int? MatchThreshold { get; init; }

    public int? MissThreshold { get; init; }

    public bool SaveDebugImages { get; init; }

    public bool SaveOcrFailureSamples { get; init; }

    public bool EnableInputForegroundFallback { get; init; }

    public static GuiDetectionTuningOptions Parse(
        bool runUntilStop,
        string? loopCount,
        string? loopIntervalMs,
        string? captureDelayMs,
        string? matchThreshold,
        string? missThreshold,
        bool saveDebugImages = false,
        bool saveOcrFailureSamples = false,
        bool enableInputForegroundFallback = false)
    {
        return new GuiDetectionTuningOptions
        {
            RunUntilStop = runUntilStop,
            LoopCount = ParseLoopCount(runUntilStop, loopCount),
            LoopIntervalMs = ParseOptionalLoopInterval(loopIntervalMs),
            CaptureDelayMs = ParseOptionalCaptureDelay(captureDelayMs),
            MatchThreshold = ParseOptionalThreshold(matchThreshold, "Match threshold"),
            MissThreshold = ParseOptionalThreshold(missThreshold, "Miss threshold"),
            SaveDebugImages = saveDebugImages,
            SaveOcrFailureSamples = saveOcrFailureSamples,
            EnableInputForegroundFallback = enableInputForegroundFallback
        };
    }

    public string FormatLoopCount(int? effectiveLoopCount)
    {
        return RunUntilStop
            ? "until stopped"
            : effectiveLoopCount?.ToString() ?? "config/default";
    }

    private static int? ParseLoopCount(bool runUntilStop, string? value)
    {
        if (runUntilStop)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Loop count must be a positive whole number when Run until Stop is unchecked.");
        }

        int loopCount = ParsePositiveWholeNumber(value, "Loop count");
        try
        {
            DetectionDryRunOptions.ValidateLoopCount(loopCount);
            return loopCount;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException("Loop count must be greater than 0.", exception);
        }
    }

    private static int? ParseOptionalLoopInterval(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        int intervalMs = ParsePositiveWholeNumber(value, "Loop interval ms");
        try
        {
            DetectionDryRunOptions.ValidateLoopIntervalMs(intervalMs);
            return intervalMs;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException(
                $"Loop interval ms must be between {DetectionDryRunOptions.MinLoopIntervalMs} and {DetectionDryRunOptions.MaxLoopIntervalMs}.",
                exception);
        }
    }

    private static int? ParseOptionalCaptureDelay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value.Trim(), out int captureDelayMs))
        {
            throw new ArgumentException("Capture delay ms must be a whole number.");
        }

        try
        {
            WindowCaptureOptions.ValidateCaptureDelayMs(captureDelayMs);
            return captureDelayMs;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException($"Capture delay ms must be between 0 and {WindowCaptureOptions.MaxCaptureDelayMs}.", exception);
        }
    }

    private static int? ParseOptionalThreshold(string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        int threshold = ParsePositiveWholeNumber(value, label);
        try
        {
            DetectionStabilityOptions.ValidateThreshold(threshold, label);
            return threshold;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new ArgumentException(
                $"{label} must be between {DetectionStabilityOptions.MinThreshold} and {DetectionStabilityOptions.MaxThreshold}.",
                exception);
        }
    }

    private static int ParsePositiveWholeNumber(string value, string label)
    {
        if (!int.TryParse(value.Trim(), out int result))
        {
            throw new ArgumentException($"{label} must be a positive whole number.");
        }

        if (result <= 0)
        {
            throw new ArgumentException($"{label} must be greater than 0.");
        }

        return result;
    }
}
