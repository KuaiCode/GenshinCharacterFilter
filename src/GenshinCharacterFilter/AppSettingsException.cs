namespace GenshinCharacterFilter;

/// <summary>
/// Represents a configuration loading or validation error.
/// </summary>
public sealed class AppSettingsException : Exception
{
    public AppSettingsException(string message)
        : base(message)
    {
    }

    public AppSettingsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
