# Roadmap

## Current Milestone: v0.1 Audio MVP

Status: project skeleton created.

The v0.1 milestone is limited to:

- console app only;
- simulated speaker input;
- target process audio mute/restore coordination;
- testable core logic without changing real system volume;
- real Windows audio control isolated behind an audio service interface when implemented later.

## Phase Order

1. Simulated speaker input and target process mute/restore.
2. Window capture prototype.
3. OCR text extraction from a configurable screen region.
4. Speaker detection from OCR text.
5. Stable mute/unmute coordination with debounce and recovery.
6. Minimal UI and local configuration.
7. Optional screen-region masking prototype.
8. Optional dynamic mask or model-based detection prototype.

## Out of Scope for This Skeleton

- OCR.
- Screen capture.
- WPF or WinUI.
- Overlay masking.
- Real audio control.
- Gameplay automation.
- Game memory access or modification.
