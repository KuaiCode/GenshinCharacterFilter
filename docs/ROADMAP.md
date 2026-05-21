# Roadmap

## Current Milestone: v0.19 WPF Persistent Control Dock / Interaction Layout

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, v0.8 Simulated Audio Integration, v0.9 Guarded Real Audio Integration, v0.10 Manual OCR Region Calibration, v0.11 OCR Region Source Resolution, v0.12 Configuration Integration, v0.13 Usability Hardening, v0.14 Minimal WinForms Control Panel, v0.15 GUI Hardening, v0.16 GUI Guarded Real Audio Page, v0.17 WPF Modern GUI Shell, v0.18 OCR Backend Replacement, and v0.18.1 OCR Backend Diagnostic Stabilization milestones have been manually verified where applicable. The v0.19 milestone is limited to:

- preserving console app and WPF GUI behavior;
- keeping real-audio safety gates unchanged;
- preserving Paddle OCR backend selection, warm-up, and cache-key behavior;
- preserving foreground-region-only capture behavior;
- adding a persistent WPF control/status dock visible across pages;
- showing run state, audio state, OCR backend/status, target process, target speakers, last OCR text, last detected speaker, and last audio action in the dock;
- exposing Start Guarded Real Audio, Stop, and Restore from the persistent dock instead of only inside the Audio page;
- keeping Overview, OCR, Detection, Audio, and Logs pages focused on configuration and diagnostics;
- no global hotkeys, tray icon, always-on-top mini window, overlay, masking, hooks, input automation, game memory access, MuteCoordinator changes, OCR backend architecture changes, or real audio enabled by default.

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
19. OCR backend replacement / low-latency OCR spike.
20. WPF persistent control dock / interaction layout.
21. Stable mute/unmute coordination with debounce and recovery.
22. Optional masking.

## Out of Scope for v0.19

- New major features.
- GUI config editor.
- Saving edited config.
- Global hotkeys.
- Tray icon.
- Always-on-top mini status window.
- Real audio enabled by default.
- Bypassing guarded real audio safety semantics.
- Changing detection/audio safety gates.
- Changing OCR backend architecture.
- Removing Tesseract CLI fallback.
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
