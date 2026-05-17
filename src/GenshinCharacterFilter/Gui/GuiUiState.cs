namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Describes the button state derived from the current GUI run state.
/// </summary>
public readonly record struct GuiUiState(
    GuiRunState RunState,
    bool CommandButtonsEnabled,
    bool StopButtonEnabled);
