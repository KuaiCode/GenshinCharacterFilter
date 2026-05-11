# Roadmap

## Current Milestone: v0.7 Detection Stability Gate

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, and v0.6 OCR-driven Detection Dry Run milestones have been manually verified. The v0.7 milestone is limited to:

- console app only;
- stability-gated OCR-driven detection dry-run output;
- requiring consecutive raw matches before stable target-present state;
- requiring consecutive raw misses before stable target-absent state;
- printing raw match result, stable match state, stable state changes, and consecutive counts;
- configurable match/miss thresholds for dry-run observation;
- preserving existing simulated and opt-in real audio behavior;
- no automatic mute/restore from OCR, no `MuteCoordinator` integration, no real audio control from stable dry-run results, no GUI, overlay, or masking.

## Phase Order

1. Simulated speaker input and target process mute/restore.
2. Real Windows audio control behind `IAudioMuteService`.
3. Audio filter modes: mute and reduce volume.
4. Local JSON configuration.
5. Window capture prototype.
6. OCR text extraction from a configurable screen region.
7. Speaker detection from OCR text.
8. OCR-driven detection dry run.
9. Detection stability gate.
10. Stable mute/unmute coordination with debounce and recovery.
11. Minimal UI.
12. Optional masking.

## Out of Scope for v0.7

- Speaker recognition from image.
- Automatic mute/restore based on OCR.
- `MuteCoordinator` integration.
- Real audio mute/restore based on OCR.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
