using GenshinCharacterFilter.Ocr;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Detection;

/// <summary>
/// Contains the observable output from one dry-run iteration.
/// </summary>
public sealed record DetectionDryRunResult(
    int Iteration,
    OcrResult OcrResult,
    SpeakerMatchResult SpeakerMatchResult,
    DetectionStabilityResult StabilityResult,
    string OcrRegionSourceLabel = "none/full image",
    DetectionAudioActionResult? DetectionAudioActionResult = null,
    DetectionIterationTiming? Timing = null);
