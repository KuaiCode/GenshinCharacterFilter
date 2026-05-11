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

## 2026-05-10: v0.4 Tesseract CLI OCR Prototype

Decision: add OCR raw text extraction behind `IOcrService`, with the first provider implemented by invoking an external Tesseract CLI process.

Reasoning:

- OCR must be explicitly triggered through `--ocr-once`; default startup remains the existing simulated audio flow.
- The provider abstraction keeps OCR replaceable and prevents Tesseract details from leaking into coordination logic.
- Tesseract CLI is used without adding a NuGet OCR dependency and without vendoring Tesseract binaries or traineddata files.
- OCR reads local image files and does not send screenshots to cloud OCR services.
- Missing Tesseract, missing language data, missing input files, and non-zero Tesseract exit codes should produce clear user-facing errors.
- OCR output is printed as raw text only and is not connected to speaker detection, `MuteCoordinator`, or automatic mute/restore behavior.
- The milestone does not add WPF, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, or input automation.

## 2026-05-10: v0.4.1 OCR Region Crop Before OCR

Decision: add optional OCR input region cropping before invoking Tesseract CLI, saving the cropped debug input image as `debug-ocr/ocr-input-latest.png`.

Reasoning:

- Whole-image OCR can include window menus, status bars, and unrelated UI text, which makes raw OCR validation noisy.
- Cropping uses image coordinates and validates the region against the input image before OCR runs.
- The cropped debug image makes the actual OCR input reviewable without adding OCR result interpretation.
- The OCR provider still receives a local image path and remains isolated behind `IOcrService`.
- The behavior does not add speaker detection from OCR text, does not connect to `MuteCoordinator`, and does not trigger automatic audio control.
- The milestone still avoids OpenCV, ONNX, cloud OCR, game memory access, hooks, injection, GUI, overlay, and masking.

## 2026-05-10: v0.4.2 Local OCR Image Preprocessing

Decision: add simple local OCR input preprocessing options for integer scale, grayscale conversion, and thresholding before invoking Tesseract CLI.

Reasoning:

- Small Chinese text often needs a larger and cleaner OCR input than the raw screenshot region.
- Preprocessing runs locally in `OcrInputPreparer` after optional region cropping and before Tesseract receives the image.
- The debug image remains `debug-ocr/ocr-input-latest.png` so users can inspect the exact OCR input.
- The implementation uses existing .NET/Windows drawing APIs and does not introduce OpenCV, ONNX, OCR model packages, or cloud OCR.
- The behavior does not add speaker detection, does not connect to `MuteCoordinator`, and does not trigger automatic audio control.

## 2026-05-11: Prefer Raw Cropped OCR Input For Current Samples

Decision: use raw cropped image OCR as the current v0.4 recommended path; keep preprocessing flags available only as optional debugging tools.

Reasoning:

- Manual OCR testing showed that a focused `--ocr-region` crop can recognize the target Chinese text correctly.
- Additional scale, grayscale, and threshold preprocessing can remove anti-aliasing or damage small Chinese stroke structure, which may make Tesseract recognition worse for the current sample.
- The current priority is to stabilize region selection and raw OCR text output before adding speaker detection from OCR text.
- OCR output remains disconnected from `MuteCoordinator` and automatic audio control.
