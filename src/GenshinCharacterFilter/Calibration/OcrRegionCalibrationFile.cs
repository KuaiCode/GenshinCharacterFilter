using System.Text.Json;

namespace GenshinCharacterFilter.Calibration;

/// <summary>
/// Saves and loads local OCR region calibration JSON.
/// </summary>
public sealed class OcrRegionCalibrationFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Saves calibration JSON as UTF-8.
    /// </summary>
    public void Save(OcrRegionCalibrationResult result, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Calibration output path cannot be empty.", nameof(outputPath));
        }

        result.Validate();

        string fullPath = Path.GetFullPath(outputPath.Trim());
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(result, JsonOptions);
        File.WriteAllText(fullPath, json);
    }

    /// <summary>
    /// Loads and validates calibration JSON.
    /// </summary>
    public OcrRegionCalibrationResult Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Calibration file path cannot be empty.", nameof(path));
        }

        string fullPath = Path.GetFullPath(path.Trim());
        if (!File.Exists(fullPath))
        {
            throw new CalibrationException($"Calibration file '{fullPath}' was not found.");
        }

        try
        {
            string json = File.ReadAllText(fullPath);
            OcrRegionCalibrationResult? result = JsonSerializer.Deserialize<OcrRegionCalibrationResult>(json, JsonOptions);
            if (result is null)
            {
                throw new CalibrationException($"Calibration file '{fullPath}' is empty or invalid.");
            }

            result.Validate();
            return result;
        }
        catch (JsonException exception)
        {
            throw new CalibrationException($"Calibration file '{fullPath}' is not valid JSON: {exception.Message}", exception);
        }
    }
}
