namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Contains per-iteration timing diagnostics for OCR-driven detection.
/// </summary>
public sealed record DetectionIterationTiming(
    long CaptureElapsedMs,
    long OcrElapsedMs,
    long MatchElapsedMs,
    long AudioElapsedMs,
    long TotalElapsedMs);
