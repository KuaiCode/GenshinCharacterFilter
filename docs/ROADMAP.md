# Roadmap

## Current Milestone: v0.2 Local JSON Configuration

Status: in progress.

The v0.1 Audio MVP has been manually verified. The v0.2 milestone is limited to:

- console app only;
- local JSON configuration;
- safe defaults when no config is supplied;
- CLI overrides for supported settings;
- preserving existing simulated and opt-in real audio behavior.

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
10. Optional screen-region masking prototype.
11. Optional dynamic mask or model-based detection prototype.

## Out of Scope for v0.2

- OCR.
- Screen capture.
- WPF or WinUI.
- Overlay masking.
- Gameplay automation.
- Game memory access or modification.
