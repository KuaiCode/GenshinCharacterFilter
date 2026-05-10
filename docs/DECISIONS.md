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
