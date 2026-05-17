using GenshinCharacterFilter.Gui;

namespace GenshinCharacterFilter.Tests;

public sealed class GuiStateControllerTests
{
    [Fact]
    public void StartsIdle()
    {
        GuiStateController controller = new();

        GuiUiState state = controller.Current;

        Assert.Equal(GuiRunState.Idle, state.RunState);
        Assert.True(state.CommandButtonsEnabled);
        Assert.False(state.StopButtonEnabled);
    }

    [Fact]
    public void StartOperation_TransitionsIdleToRunning()
    {
        GuiStateController controller = new();

        GuiUiState state = controller.StartOperation(cancellable: true);

        Assert.Equal(GuiRunState.Running, state.RunState);
        Assert.False(state.CommandButtonsEnabled);
        Assert.True(state.StopButtonEnabled);
    }

    [Fact]
    public void RequestStop_TransitionsRunningToStopping()
    {
        GuiStateController controller = new();
        controller.StartOperation(cancellable: true);

        GuiUiState state = controller.RequestStop();

        Assert.Equal(GuiRunState.Stopping, state.RunState);
        Assert.False(state.CommandButtonsEnabled);
        Assert.False(state.StopButtonEnabled);
    }

    [Fact]
    public void CompleteOperation_TransitionsStoppingToIdle()
    {
        GuiStateController controller = new();
        controller.StartOperation(cancellable: true);
        controller.RequestStop();

        GuiUiState state = controller.CompleteOperation();

        Assert.Equal(GuiRunState.Idle, state.RunState);
        Assert.True(state.CommandButtonsEnabled);
        Assert.False(state.StopButtonEnabled);
    }

    [Fact]
    public void FailOperation_TransitionsRunningToErrorAndRecoversButtons()
    {
        GuiStateController controller = new();
        controller.StartOperation(cancellable: true);

        GuiUiState state = controller.FailOperation();

        Assert.Equal(GuiRunState.Error, state.RunState);
        Assert.True(state.CommandButtonsEnabled);
        Assert.False(state.StopButtonEnabled);
    }

    [Fact]
    public void NonCancellableOperation_DisablesStop()
    {
        GuiStateController controller = new();

        GuiUiState state = controller.StartOperation(cancellable: false);

        Assert.Equal(GuiRunState.Running, state.RunState);
        Assert.False(state.CommandButtonsEnabled);
        Assert.False(state.StopButtonEnabled);
    }
}
