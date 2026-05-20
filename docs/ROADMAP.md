# Roadmap

## Current Milestone: v0.17 WPF Modern GUI Shell

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, v0.8 Simulated Audio Integration, v0.9 Guarded Real Audio Integration, v0.10 Manual OCR Region Calibration, v0.11 OCR Region Source Resolution, v0.12 Configuration Integration, v0.13 Usability Hardening, v0.14 Minimal WinForms Control Panel, v0.15 GUI Hardening, and v0.16 GUI Guarded Real Audio Page milestones have been manually verified where applicable. The v0.17 milestone is limited to:

- preserving console app behavior;
- keeping GUI launch explicit via `--gui`;
- adding a modern WPF shell over existing core services;
- launching the WPF shell from `--gui` while leaving existing CLI behavior unchanged;
- keeping the existing WinForms calibration selector and old WinForms MainForm available temporarily where useful;
- separating Overview, Config, OCR, Detection, Audio, and Logs pages with left navigation and a top status bar;
- using card-style panels, a readable log page, light/dark startup theme detection, and an optional built-in Windows backdrop effect;
- making guarded real audio visually distinct as a danger zone;
- preserving the existing guarded real audio checkbox, warning, confirmation, preflight, and stable-detection safety rules;
- preserving the existing live capture, fixed-image debug option, GUI tuning, Save debug images option, region-only capture, and timing diagnostics;
- sharing Stop with existing long-running operations and preserving cleanup/restore behavior;
- preserving existing simulated detection audio GUI behavior;
- preserving existing CLI behavior;
- preserving existing simulated and opt-in real audio behavior;
- no third-party UI dependencies, WinUI, GUI config editor, config saving, automatic region detection, fabricated preset coordinates, overlay, or masking.

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
17. GUI guarded real audio page.
18. WPF modern GUI shell.
19. Stable mute/unmute coordination with debounce and recovery.
20. Optional masking.

## Out of Scope for v0.17

- New major features.
- Speaker recognition from image.
- GUI config editor.
- Saving edited config.
- Real audio enabled by default.
- Bypassing guarded real audio safety semantics.
- New calibration UI features.
- Automatic OCR region detection.
- Fabricated preset coordinates.
- Real audio without existing guarded real-audio flags.
- MuteCoordinator changes.
- Fuzzy matching unless explicitly requested.
- WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
