# GenshinCharacterFilter

GenshinCharacterFilter is a planned Windows desktop accessibility/preferences utility for muting or reducing game audio when a configured character is speaking.

Current milestone: **v0.1 Audio MVP**.

This repository currently contains only the initial .NET 8 project skeleton:

- `src/GenshinCharacterFilter`: console application.
- `tests/GenshinCharacterFilter.Tests`: focused test project placeholder.
- `docs`: roadmap and architectural decisions.

## Current Scope

The current phase is limited to console-based simulated speaker input and target process mute/restore coordination. This skeleton does not implement detection, real audio control, OCR, screen capture, overlay, WPF, or WinUI.

## Commands

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj
```

## Dependency Policy

No audio, OCR, image-processing, model-inference, capture, UI, or overlay packages are included in this skeleton. Test framework dependencies are limited to what is needed for `dotnet test`.
