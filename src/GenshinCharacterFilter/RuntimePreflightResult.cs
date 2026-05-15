namespace GenshinCharacterFilter;

/// <summary>
/// Collects preflight validation results before a runtime command starts.
/// </summary>
public sealed class RuntimePreflightResult
{
    public RuntimePreflightResult(IEnumerable<RuntimePreflightIssue> issues)
    {
        Issues = [.. issues];
    }

    public IReadOnlyList<RuntimePreflightIssue> Issues { get; }

    public bool Passed => Issues.Count == 0;
}
