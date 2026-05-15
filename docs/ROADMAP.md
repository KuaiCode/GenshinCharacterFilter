# Roadmap

## Current Milestone: v0.8 Simulated Audio Integration

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, and v0.7 Detection Stability Gate milestones have been manually verified. The v0.8 milestone is limited to:

- console app only;
- simulated detection audio mode;
- using stable detection result, not raw match, to drive simulated audio actions;
- printing raw match, stable match, and simulated audio action;
- rejecting or ignoring real audio for simulated detection audio mode;
- preserving existing simulated and opt-in real audio behavior;
- no `WindowsAudioMuteService` integration from OCR, no real audio control from stable detection results, no GUI, overlay, or masking.

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
11. Stable mute/unmute coordination with debounce and recovery.
12. Minimal UI.
13. Optional masking.

## Out of Scope for v0.8

- Speaker recognition from image.
- Real audio mute/restore based on OCR.
- `WindowsAudioMuteService` integration from detection.
- Production auto mute.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
