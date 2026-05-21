# Roadmap

## Current Milestone: v0.18 OCR Backend Replacement / Low-latency OCR Spike

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, v0.8 Simulated Audio Integration, v0.9 Guarded Real Audio Integration, v0.10 Manual OCR Region Calibration, v0.11 OCR Region Source Resolution, v0.12 Configuration Integration, v0.13 Usability Hardening, v0.14 Minimal WinForms Control Panel, v0.15 GUI Hardening, v0.16 GUI Guarded Real Audio Page, and v0.17 WPF Modern GUI Shell milestones have been manually verified where applicable. The v0.18 milestone is limited to:

- preserving console app and WPF GUI behavior;
- keeping real-audio safety gates unchanged;
- keeping `TesseractCli` as the default OCR backend and fallback;
- adding an optional `PaddleOcrLocal` backend through the existing `IOcrService` abstraction;
- allowing OCR backend selection from config or CLI;
- allowing WPF GUI OCR backend selection for the current run;
- adding a WPF OCR backend warm-up action so Paddle can initialize before detection starts;
- reusing the PaddleOCR engine across detection-loop iterations instead of launching an external executable each frame;
- reusing the selected Paddle backend across WPF Start/Stop cycles instead of recreating it for each operation;
- preserving OCR timing diagnostics so backend latency can be compared;
- adding `--ocr-benchmark` to compare raw text, first-run timing, and warm-run timing on the same crop;
- adding optional OCR failure sample capture under `debug-ocr/failures`;
- adding explicit, opt-in OCR input preparation settings while keeping raw crop as the default;
- reporting clear errors for missing Paddle native runtime/model files or unsupported architecture;
- no overlay, masking, hooks, input automation, game memory access, MuteCoordinator changes, or real audio enabled by default.

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
20. Stable mute/unmute coordination with debounce and recovery.
21. Optional masking.

## Out of Scope for v0.18

- New major features.
- GUI config editor.
- Saving edited config.
- Real audio enabled by default.
- Bypassing guarded real audio safety semantics.
- Changing detection/audio safety gates.
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
