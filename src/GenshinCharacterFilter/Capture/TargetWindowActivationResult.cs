namespace GenshinCharacterFilter.Capture;

public sealed record TargetWindowActivationResult(
    bool Success,
    TargetWindowActivationFailureReason FailureReason,
    string UserMessage,
    TargetWindowActivationMethod Method,
    bool InputFallbackAttempted)
{
    public static TargetWindowActivationResult Succeeded(TargetWindowActivationMethod method)
    {
        return new TargetWindowActivationResult(
            true,
            TargetWindowActivationFailureReason.None,
            $"Target window activation succeeded with {method}.",
            method,
            method == TargetWindowActivationMethod.InputFallback);
    }

    public static TargetWindowActivationResult Failed(
        TargetWindowActivationFailureReason reason,
        string userMessage,
        bool inputFallbackAttempted = false)
    {
        return new TargetWindowActivationResult(
            false,
            reason,
            userMessage,
            TargetWindowActivationMethod.None,
            inputFallbackAttempted);
    }
}
