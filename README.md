# GenshinCharacterFilter

GenshinCharacterFilter is a planned Windows desktop accessibility/preferences utility for muting or reducing game audio when a configured character is speaking.

Current milestone: **v0.1 Audio MVP**.

This repository currently contains the initial .NET 8 project skeleton and testable v0.1 mute coordination core:

- `src/GenshinCharacterFilter`: console application.
- `tests/GenshinCharacterFilter.Tests`: focused tests for simulated speaker coordination.
- `docs`: roadmap and architectural decisions.

## Current Scope

The current phase is limited to console-based simulated speaker input and target process mute/restore coordination. It defines `ISpeakerDetector`, `IAudioMuteService`, and `MuteCoordinator` so the state logic can be tested without changing real system volume.

This build does not control real Windows audio and does not implement OCR, screen capture, overlay, WPF, or WinUI.

## Commands

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj
```

## Dependency Policy

No audio, OCR, image-processing, model-inference, capture, UI, or overlay packages are included. Test framework dependencies are limited to what is needed for `dotnet test`.
