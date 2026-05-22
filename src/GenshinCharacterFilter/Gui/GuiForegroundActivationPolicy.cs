using GenshinCharacterFilter.Capture;

namespace GenshinCharacterFilter.Gui;

public static class GuiForegroundActivationPolicy
{
    public static bool ShouldTryInputFallback(TargetWindowActivationResult result, bool inputFallbackEnabled)
    {
        ArgumentNullException.ThrowIfNull(result);
        return inputFallbackEnabled &&
            !result.Success &&
            result.FailureReason != TargetWindowActivationFailureReason.TargetNotFound;
    }

    public static bool ShouldUseManualFallback(TargetWindowActivationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return !result.Success &&
            result.FailureReason != TargetWindowActivationFailureReason.TargetNotFound;
    }

    public static bool ShouldFailImmediately(TargetWindowActivationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return !result.Success &&
            result.FailureReason == TargetWindowActivationFailureReason.TargetNotFound;
    }
}
