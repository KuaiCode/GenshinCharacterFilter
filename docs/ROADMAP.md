# Roadmap

## Current Milestone: v0.20 Capture Backend Spike / BetterGI-style Capture Backend Evaluation

Status: in progress.

The v0.1 Audio MVP through v0.19.2 Foreground UX / Resume Flow milestones are implemented or stage-complete where applicable. v0.19.2 improved foreground activation, optional input fallback, and Resume/Reconnect, but manual testing still showed `StillMinimized` and `SendInput error: 87` failures. v0.20 shifts focus to capture backend abstraction and a Windows.Graphics.Capture spike instead of more foreground-switching patches.

The v0.20 milestone is limited to:

- preserving console app and WPF GUI behavior;
- keeping real-audio safety gates unchanged;
- preserving Paddle OCR backend selection, warm-up, and cache-key behavior;
- preserving the existing VisiblePixels / foreground-region-only capture backend;
- adding a capture backend abstraction used by calibration, live OCR/detection, simulated audio, and guarded real audio;
- adding config/GUI selection for `VisiblePixels` and `WindowsGraphicsCapture`;
- adding explicit backend fallback policy from `WindowsGraphicsCapture` to `VisiblePixels`;
- logging selected capture backend and capture mode;
- keeping fixed-image OCR independent from live capture backend;
- adding a Windows.Graphics.Capture spike with clear diagnostics if WGC is unavailable or frame acquisition is not enabled;
- no global hotkeys, tray icon, always-on-top mini window, overlay, masking, hooks, gameplay automation, game memory access, MuteCoordinator changes, OCR backend architecture changes, DirectX hooks, or real audio enabled by default.

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
20. v0.19 WPF persistent control dock / interaction layout.
21. v0.19.1 CaptureLost UI recovery.
22. v0.19.2 Foreground UX / Resume Flow.
23. v0.20 Capture backend spike / Windows.Graphics.Capture evaluation.
24. Future DXGI / BitBlt backend evaluation if Windows.Graphics.Capture is insufficient.
25. Future global hotkey / tray / status mini-window.
26. Stable mute/unmute coordination with debounce and recovery.
27. Optional masking.

## Out of Scope for v0.20

- New major features.
- GUI config editor.
- Saving edited config.
- Global hotkeys.
- Tray icon.
- Always-on-top mini status window.
- DXGI / BitBlt backend implementation unless explicitly scoped later.
- DirectX hooks.
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
