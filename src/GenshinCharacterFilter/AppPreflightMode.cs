namespace GenshinCharacterFilter;

/// <summary>
/// Selects which runtime checks should run before starting a command.
/// </summary>
public enum AppPreflightMode
{
    Command,
    ValidateConfig
}
