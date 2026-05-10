# Roadmap

## Current Milestone: v0.3 Window Capture Prototype

Status: in progress.

The v0.1 Audio MVP and v0.2 Local JSON Configuration milestones have been manually verified. The v0.3 milestone is limited to:

- console app only;
- finding a target process/window;
- capturing the target window or configured screen region;
- saving a debug screenshot to a local debug folder;
- preserving existing simulated and opt-in real audio behavior;
- no OCR, speaker recognition from image, GUI, overlay, or masking.

## Phase Order

1. Simulated speaker input and target process mute/restore.
2. Real Windows audio control behind `IAudioMuteService`.
3. Audio filter modes: mute and reduce volume.
4. Local JSON configuration.
5. Window capture prototype.
6. OCR text extraction from a configurable screen region.
7. Speaker detection from OCR text.
8. Stable mute/unmute coordination with debounce and recovery.
9. Minimal UI.
10. Optional masking.

## Out of Scope for v0.3

- OCR.
- Speaker recognition from image.
- WPF or WinUI.
- Overlay masking.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
