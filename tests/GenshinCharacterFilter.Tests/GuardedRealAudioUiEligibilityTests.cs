using GenshinCharacterFilter.Gui;

namespace GenshinCharacterFilter.Tests;

public sealed class GuardedRealAudioUiEligibilityTests
{
    [Fact]
    public void CanRequestConfirmation_WhenAllRequirementsAreMet()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: true,
            OperationActive: false,
            PreflightPassed: true,
            HasOcrRegionSource: true,
            HasTargetProcess: true);

        Assert.True(eligibility.CanRequestConfirmation);
        Assert.Equal(string.Empty, eligibility.DisabledReason);
    }

    [Fact]
    public void CannotRequestConfirmation_WhenCheckboxIsNotChecked()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: false,
            OperationActive: false,
            PreflightPassed: true,
            HasOcrRegionSource: true,
            HasTargetProcess: true);

        Assert.False(eligibility.CanRequestConfirmation);
        Assert.Contains("Enable guarded real audio", eligibility.DisabledReason);
    }

    [Fact]
    public void CannotRequestConfirmation_WhenOperationIsActive()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: true,
            OperationActive: true,
            PreflightPassed: true,
            HasOcrRegionSource: true,
            HasTargetProcess: true);

        Assert.False(eligibility.CanRequestConfirmation);
        Assert.Contains("operation is running", eligibility.DisabledReason);
    }

    [Fact]
    public void CannotRequestConfirmation_WhenPreflightFails()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: true,
            OperationActive: false,
            PreflightPassed: false,
            HasOcrRegionSource: true,
            HasTargetProcess: true);

        Assert.False(eligibility.CanRequestConfirmation);
        Assert.Contains("Preflight", eligibility.DisabledReason);
    }

    [Fact]
    public void CannotRequestConfirmation_WhenTargetProcessIsMissing()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: true,
            OperationActive: false,
            PreflightPassed: true,
            HasOcrRegionSource: true,
            HasTargetProcess: false);

        Assert.False(eligibility.CanRequestConfirmation);
        Assert.Contains("Target process", eligibility.DisabledReason);
    }

    [Fact]
    public void CannotRequestConfirmation_WhenOcrRegionSourceIsMissing()
    {
        GuardedRealAudioUiEligibility eligibility = new(
            EnableChecked: true,
            OperationActive: false,
            PreflightPassed: true,
            HasOcrRegionSource: false,
            HasTargetProcess: true);

        Assert.False(eligibility.CanRequestConfirmation);
        Assert.Contains("OCR region source", eligibility.DisabledReason);
    }
}
