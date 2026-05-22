namespace GenshinCharacterFilter.Capture;

public interface ITargetWindowActivator
{
    Task<TargetWindowActivationResult> TryActivateTargetWindowAsync(
        string processName,
        int delayMs,
        TargetWindowActivationOptions options,
        CancellationToken cancellationToken);
}
