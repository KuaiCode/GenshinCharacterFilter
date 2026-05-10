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

## 2026-05-10: Interactive Simulation Before Real Audio

Decision: add a lightweight interactive console mode using `ManualSpeakerDetector` and `LoggingAudioMuteService` before implementing real Windows audio control.

Reasoning:

- The v0.1 milestone needs a command-line verifiable vertical slice.
- Manual speaker input exercises the same `MuteCoordinator` used by tests.
- Logging audio requests keeps verification safe and reversible because no system volume is changed.
- Real Windows audio remains deferred behind `IAudioMuteService`.

## 2026-05-10: Real Audio Behind IAudioMuteService

Decision: add `WindowsAudioMuteService` behind `IAudioMuteService`, while keeping the default console mode simulated unless `--real-audio` is explicitly supplied.

Reasoning:

- Real audio control belongs behind `IAudioMuteService` so `MuteCoordinator` remains testable without changing system volume.
- NAudio is used to access Windows Core Audio render sessions for a target process.
- The implementation mutes only matched process audio sessions and attempts to restore each session's previous mute state and volume.
- Deployment now includes NAudio managed NuGet assemblies for real audio mode.
- The approach does not inject into the game process, read or write game memory, hook the process, modify game files, or simulate keyboard/mouse input.

## 2026-05-10: Audio Action Supports Mute and Reduce Volume

Decision: introduce `AudioFilterMode` and `AudioFilterOptions` so target-speaker audio handling can either mute or reduce volume while restore keeps the original session state.

Reasoning:

- Mute remains the default behavior for backwards compatibility and safety.
- Reduce-volume mode uses the volume captured at first trigger, so repeated target frames do not repeatedly reduce volume.
- Restore returns the original mute flag and volume when the session is still available.
- The behavior stays behind `IAudioMuteService`; `MuteCoordinator` remains unchanged.

## 2026-05-10: v0.2 Local JSON Configuration

Decision: add local JSON configuration through `AppSettings` and `AppSettingsLoader`; CLI arguments may override JSON values.

Reasoning:

- Defaults must remain safe: real audio is disabled unless config or CLI explicitly enables it.
- Local JSON keeps configuration transparent and reviewable without adding a settings UI.
- CLI overrides allow quick manual verification without editing config files.
- Invalid or missing config files should produce clear errors instead of silent fallback.
- Configuration stays separate from `Program.cs`, which only performs light loading, merging, and service wiring.

## 2026-05-10: v0.3 Debug Window Screenshot First

Decision: add a one-shot window capture prototype behind `IGameWindowCapture`, saving debug screenshots locally before adding OCR or image-based speaker recognition.

Reasoning:

- Debug screenshots make the capture boundary manually verifiable before OCR exists.
- Screenshot mode is explicit through `--capture-once` and does not control real system audio.
- Full-window capture uses DWM extended frame bounds first, then falls back to `GetWindowRect`, so the default debug screenshot includes the title bar and visible frame.
- v0.3 uses visible-window screen capture; it first attempts foreground activation plus a short delay to reduce accidental capture of the terminal covering the target.
- The main project and test project target `net8.0-windows`, and the main project references `Microsoft.WindowsDesktop.App`, because the prototype uses Windows-only capture, drawing, and PNG debug output APIs.
- The Windows Desktop framework reference is a build/runtime boundary for capture support only; it does not move the project into WPF, WinUI, or GUI work.
- Background DirectX capture, window-content capture, hooks, and process injection are intentionally out of scope.
- Win32 capture details stay isolated in `WindowsGameWindowCapture` instead of `Program.cs`.
- The milestone does not add OCR, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, or input automation.
