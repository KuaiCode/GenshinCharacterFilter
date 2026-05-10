# Roadmap

## Current Milestone: v0.4 OCR Text Extraction Prototype

Status: in progress.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, and v0.3 Window Capture Prototype milestones have been manually verified. The v0.4 milestone is limited to:

- console app only;
- explicitly triggered OCR raw text extraction;
- using an existing screenshot or captured image as OCR input;
- printing raw OCR text to the console;
- isolating OCR behind `IOcrService`;
- preserving existing simulated and opt-in real audio behavior;
- no speaker detection from OCR text, automatic mute/restore from OCR, GUI, overlay, or masking.

## Phase Order

1. Simulated speaker input and target process mute/restore.
2. Real Windows audio control behind `IAudioMuteService`.
3. Audio filter modes: mute and reduce volume.
4. Local JSON configuration.
5. Window capture prototype.
6. OCR text extraction from a configurable screen region.
7. Speaker detection from OCR text.
8. Stable mute/unmute coordination with debounce and recovery.
9. Minimal UI.
10. Optional masking.

## Out of Scope for v0.4

- Speaker recognition from image.
- Speaker detection from OCR text.
- Automatic mute/restore based on OCR.
- WPF or WinUI.
- Overlay masking.
- ONNX or OpenCV unless explicitly justified.
- Gameplay automation.
- Game memory access or modification.
- Hooking or injection.
