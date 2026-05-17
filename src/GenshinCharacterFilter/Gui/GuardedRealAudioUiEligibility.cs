namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Describes whether the guarded real-audio UI can enter its confirmation flow.
/// </summary>
public readonly record struct GuardedRealAudioUiEligibility(
    bool EnableChecked,
    bool OperationActive,
    bool PreflightPassed,
    bool HasOcrRegionSource,
    bool HasTargetProcess)
{
    public bool CanRequestConfirmation =>
        EnableChecked &&
        !OperationActive &&
        PreflightPassed &&
        HasOcrRegionSource &&
        HasTargetProcess;

    public string DisabledReason
    {
        get
        {
            if (!EnableChecked)
            {
                return "Enable guarded real audio first.";
            }

            if (OperationActive)
            {
                return "Another operation is running.";
            }

            if (!HasTargetProcess)
            {
                return "Target process is required.";
            }

            if (!HasOcrRegionSource)
            {
                return "OCR region source is required.";
            }

            if (!PreflightPassed)
            {
                return "Preflight checks must pass.";
            }

            return string.Empty;
        }
    }
}
