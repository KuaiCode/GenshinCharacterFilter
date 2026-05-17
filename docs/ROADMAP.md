# Roadmap

## Current Milestone: v0.15 GUI Hardening

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, v0.8 Simulated Audio Integration, v0.9 Guarded Real Audio Integration, v0.10 Manual OCR Region Calibration, v0.11 OCR Region Source Resolution, v0.12 Configuration Integration, v0.13 Usability Hardening, and v0.14 Minimal WinForms Control Panel milestones have been manually verified where applicable. The v0.15 milestone is limited to:

- preserving console app behavior;
- keeping GUI launch explicit via `--gui`;
- hardening the minimal WinForms control panel;
- adding clear UI state display for Idle, Running, Stopping, and Error;
- improving button state management during running/stopping operations;
- preventing overlapping detection starts;
- logging Browse and button errors in the UI;
- auto-scrolling logs;
- improving Stop/Close cancellation behavior;
- preserving existing CLI behavior;
- keeping `--real-audio` and `--allow-real-audio-from-detection` as explicit CLI-only safety gates;
- preserving existing simulated and opt-in real audio behavior;
- no new dependencies, WPF, WinUI, GUI settings editor, automatic region detection, fabricated preset coordinates, full GUI application, overlay, or masking.

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
13. OCR region source resolution.
14. Configuration integration.
15. Minimal WinForms control panel.
16. GUI hardening.
17. Stable mute/unmute coordination with debounce and recovery.
18. Optional masking.

## Out of Scope for v0.15

- New major features.
- Speaker recognition from image.
- GUI settings editor.
- Guarded real audio GUI activation.
- New calibration UI features.
- Automatic OCR region detection.
- Fabricated preset coordinates.
- Real audio without existing guarded real-audio flags.
- MuteCoordinator changes.
- Full GUI application.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
