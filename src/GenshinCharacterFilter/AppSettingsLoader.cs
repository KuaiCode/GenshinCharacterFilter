using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenshinCharacterFilter;

/// <summary>
/// Loads and validates local JSON application settings.
/// </summary>
public sealed class AppSettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Creates safe default settings.
    /// </summary>
    public AppSettings LoadDefault()
    {
        AppSettings settings = AppSettings.CreateDefault();
        settings.Validate();
        return settings;
    }

    /// <summary>
    /// Loads settings from a JSON file.
    /// </summary>
    public AppSettings LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new AppSettingsException("Config path cannot be empty.");
        }

        if (!File.Exists(path))
        {
            throw new AppSettingsException($"Config file not found: {path}");
        }

        try
        {
            string json = File.ReadAllText(path);
            AppSettings? settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings is null)
            {
                throw new AppSettingsException($"Config file is empty or invalid: {path}");
            }

            settings.Validate();
            return settings;
        }
        catch (JsonException exception)
        {
            throw new AppSettingsException($"Config file contains invalid JSON: {path}. {exception.Message}", exception);
        }
    }
}
