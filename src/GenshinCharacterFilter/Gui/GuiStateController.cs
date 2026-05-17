namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Keeps WinForms run-state decisions testable without launching the UI.
/// </summary>
public sealed class GuiStateController
{
    public GuiRunState RunState { get; private set; } = GuiRunState.Idle;

    public bool OperationActive { get; private set; }

    public bool OperationCancellable { get; private set; }

    public GuiUiState Current => BuildState();

    public GuiUiState StartOperation(bool cancellable)
    {
        OperationActive = true;
        OperationCancellable = cancellable;
        RunState = GuiRunState.Running;
        return BuildState();
    }

    public GuiUiState RequestStop()
    {
        if (!OperationActive || !OperationCancellable)
        {
            return BuildState();
        }

        OperationCancellable = false;
        RunState = GuiRunState.Stopping;
        return BuildState();
    }

    public GuiUiState CompleteOperation()
    {
        OperationActive = false;
        OperationCancellable = false;
        RunState = GuiRunState.Idle;
        return BuildState();
    }

    public GuiUiState FailOperation()
    {
        OperationActive = false;
        OperationCancellable = false;
        RunState = GuiRunState.Error;
        return BuildState();
    }

    private GuiUiState BuildState()
    {
        return new GuiUiState(
            RunState,
            CommandButtonsEnabled: !OperationActive,
            StopButtonEnabled: OperationActive && OperationCancellable && RunState == GuiRunState.Running);
    }
}
