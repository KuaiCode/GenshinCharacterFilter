namespace GenshinCharacterFilter.Capture;

public sealed class TargetWindowActivationOptions
{
    public bool EnableInputForegroundFallback { get; init; }

    public bool VerifyForegroundProcess { get; init; } = true;
}
