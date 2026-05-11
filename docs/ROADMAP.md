# Roadmap

## Current Milestone: v0.6 OCR-driven Detection Dry Run

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, and v0.5 Speaker Detection from OCR Text Prototype milestones have been manually verified. The v0.6 milestone is limited to:

- console app only;
- explicitly triggered OCR-driven detection dry-run output;
- repeatedly OCRing a provided image or repeatedly capturing a target window before OCR;
- matching OCR raw text against configured target speakers;
- printing OCR raw text, normalized text, matched/not matched, matched speaker, and state changes;
- basic loop interval and loop count options;
- preserving existing simulated and opt-in real audio behavior;
- no automatic mute/restore from OCR, no `MuteCoordinator` integration, no real audio control from dry-run results, no production debounce/hysteresis, no GUI, overlay, or masking.

## Phase Order

1. Simulated speaker input and target process mute/restore.
2. Real Windows audio control behind `IAudioMuteService`.
3. Audio filter modes: mute and reduce volume.
4. Local JSON configuration.
5. Window capture prototype.
6. OCR text extraction from a configurable screen region.
7. Speaker detection from OCR text.
8. OCR-driven detection dry run.
9. Stable mute/unmute coordination with debounce and recovery.
10. Minimal UI.
11. Optional masking.

## Out of Scope for v0.6

- Speaker recognition from image.
- Automatic mute/restore based on OCR.
- `MuteCoordinator` integration.
- Real audio mute/restore based on OCR.
- Production OCR jitter debounce/hysteresis.
- Fuzzy matching unless explicitly requested.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
