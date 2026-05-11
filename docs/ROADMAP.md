# Roadmap

## Current Milestone: v0.5 Speaker Detection from OCR Text Prototype

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, and v0.4 OCR Text Extraction Prototype milestones have been manually verified. The v0.5 milestone is limited to:

- console app only;
- explicitly triggered speaker detection debug output;
- accepting manual text through `--speaker-text` or OCR raw text from the existing OCR path;
- normalizing OCR/manual text;
- matching against configured target speakers;
- printing matched/not matched debug results;
- preserving existing simulated and opt-in real audio behavior;
- no automatic mute/restore from OCR, no `MuteCoordinator` integration, no fuzzy matching, no OCR debounce/hysteresis, no GUI, overlay, or masking.

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

## Out of Scope for v0.5

- Speaker recognition from image.
- Automatic mute/restore based on OCR.
- `MuteCoordinator` integration.
- OCR jitter debounce/hysteresis.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
