# Roadmap

## Current Milestone: v0.10 Manual OCR Region Calibration

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, v0.8 Simulated Audio Integration, and v0.9 Guarded Real Audio Integration milestones have been manually verified where applicable. The v0.10 milestone is limited to:

- console app only;
- explicit manual OCR region calibration mode;
- capturing one target process/window screenshot;
- displaying a minimal local calibration window for drag-selecting the speaker-name OCR region;
- saving source screenshot size, pixel region, and ratio region to local JSON;
- preserving existing simulated and opt-in real audio behavior;
- no OCR, real audio control, `MuteCoordinator`, full GUI application, overlay, or masking in calibration mode.

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
10. Simulated audio integration.
11. Guarded real audio integration.
12. Manual OCR region calibration.
13. Stable mute/unmute coordination with debounce and recovery.
14. Minimal UI.
15. Optional masking.

## Out of Scope for v0.10

- Speaker recognition from image.
- OCR during calibration unless explicitly requested in a later task.
- Real audio during calibration.
- MuteCoordinator integration during calibration.
- Full GUI application.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
