# Roadmap

## Current Milestone: v0.9 Guarded Real Audio Integration

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, and v0.8 Simulated Audio Integration milestones have been manually verified. The v0.9 milestone is limited to:

- console app only;
- guarded real detection audio mode;
- requiring `--detect-loop`, `--real-audio`, `--allow-real-audio-from-detection`, and `--process <target>`;
- using stable detection result, not raw match, to drive real audio actions;
- printing warnings, target process, audio mode, stable match, and real audio action;
- preserving simulated detection audio mode;
- preserving existing simulated and opt-in real audio behavior;
- no default real audio control, no unguarded real audio control from stable detection results, no GUI, overlay, or masking.

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
12. Stable mute/unmute coordination with debounce and recovery.
13. Minimal UI.
14. Optional masking.

## Out of Scope for v0.9

- Speaker recognition from image.
- Default automatic real audio.
- Real audio without explicit allow flag.
- Production auto mute.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
