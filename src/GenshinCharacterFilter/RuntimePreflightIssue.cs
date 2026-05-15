namespace GenshinCharacterFilter;

/// <summary>
/// Describes one preflight validation problem with a user-facing category.
/// </summary>
public sealed record RuntimePreflightIssue(string Category, string Message);
