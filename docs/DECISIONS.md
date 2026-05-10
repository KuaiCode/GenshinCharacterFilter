# Decisions

## 2026-05-10: Initial .NET Skeleton

Decision: create a .NET 8 solution with a console application under `src/GenshinCharacterFilter` and an xUnit test project under `tests/GenshinCharacterFilter.Tests`.

Reasoning:

- .NET 8 matches the v0.1 Audio MVP target.
- A console app keeps the first milestone command-line verifiable.
- A test project is available before mute coordination logic is added.
- No real audio, OCR, screen capture, overlay, WPF, WinUI, or model dependencies are introduced.

## 2026-05-10: No Real Audio Dependency Yet

Decision: do not add NAudio or Windows Core Audio interop code during the skeleton task.

Reasoning:

- The task is limited to project setup.
- Real audio control must be isolated behind an interface in a later implementation step.
- Avoiding premature dependencies keeps deployment and build behavior simple.

## 2026-05-10: Fake-First Mute Coordination

Decision: implement `MuteCoordinator` against `ISpeakerDetector` and `IAudioMuteService` first, with focused tests using fake detector and fake audio service implementations.

Reasoning:

- The v0.1 milestone requires simulated speaker input before real Windows audio control.
- Fake services verify mute/restore state transitions without changing system volume.
- Real Windows audio can be added later behind `IAudioMuteService` without changing coordinator logic.
- No NAudio, OCR, screen capture, UI, overlay, OpenCV, or ONNX dependencies are needed for this step.
