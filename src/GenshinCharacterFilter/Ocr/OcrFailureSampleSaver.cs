using System.Text.Json;
using GenshinCharacterFilter.Speakers;

namespace GenshinCharacterFilter.Ocr;

/// <summary>
/// Saves diagnostic OCR crops and metadata when realtime OCR misses configured speakers.
/// </summary>
public sealed class OcrFailureSampleSaver
{
    public const string DefaultFailureDirectory = "debug-ocr/failures";

    private readonly string _outputDirectory;

    public OcrFailureSampleSaver(string outputDirectory = DefaultFailureDirectory)
    {
        _outputDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? DefaultFailureDirectory
            : outputDirectory;
    }

    public OcrFailureSampleResult Save(string imagePath, OcrFailureSampleMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new OcrException("OCR failure sample image path cannot be empty.");
        }

        if (!File.Exists(imagePath))
        {
            throw new OcrException($"OCR failure sample image was not found: {imagePath}");
        }

        Directory.CreateDirectory(_outputDirectory);
        string timestamp = metadata.Timestamp.ToLocalTime().ToString("yyyyMMdd-HHmmss");
        string stem = $"{timestamp}-iteration-{metadata.Iteration:D4}";
        string imageOutputPath = Path.Combine(_outputDirectory, $"{stem}.png");
        string metadataOutputPath = Path.Combine(_outputDirectory, $"{stem}.json");

        File.Copy(imagePath, imageOutputPath, overwrite: true);
        string json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(metadataOutputPath, json);
        return new OcrFailureSampleResult(imageOutputPath, metadataOutputPath);
    }

    public static bool ShouldSave(SpeakerMatchResult matchResult)
    {
        ArgumentNullException.ThrowIfNull(matchResult);
        return !matchResult.Matched ||
            string.IsNullOrWhiteSpace(matchResult.RawText) ||
            matchResult.MatchKind == SpeakerMatchKind.Unknown;
    }
}

public sealed record OcrFailureSampleResult(string ImagePath, string MetadataPath);

public sealed record OcrFailureSampleMetadata(
    DateTimeOffset Timestamp,
    string OcrEngine,
    string RawText,
    string NormalizedText,
    IReadOnlyCollection<string> TargetSpeakers,
    OcrRegion? Region,
    long OcrElapsedMs,
    int Iteration);
