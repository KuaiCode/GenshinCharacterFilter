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

## 2026-05-11: Prefer Raw Cropped OCR Input For Current Samples

Decision: use raw cropped image OCR as the current v0.4.1 path and do not keep scale, grayscale, or threshold preprocessing in the prototype.

Reasoning:

- Manual OCR testing showed that a focused `--ocr-region` crop can recognize the target Chinese text correctly.
- Additional scale, grayscale, and threshold preprocessing can remove anti-aliasing or damage small Chinese stroke structure, which made Tesseract recognition worse for the current sample.
- The current priority is to stabilize region selection and raw OCR text output before adding speaker detection from OCR text.
- OCR output remains disconnected from `MuteCoordinator` and automatic audio control.

## 2026-05-11: v0.5 Rule-Based Speaker Matching Debug

Decision: add simple rule-based speaker matching from manual text or OCR raw text, exposed only through explicit debug commands.

Reasoning:

- v0.5 needs to verify whether OCR raw text can identify a configured target speaker before any automatic audio behavior is considered.
- Matching is limited to normalization, exact matching, and simple contains matching to reduce false positives.
- Contains matching is allowed only for debug output in v0.5 and must not directly drive automatic mute/restore.
- Before auto mute integration, matching must be gated by stricter speaker-label parsing, OCR region confidence, debounce/hysteresis, or an explicit safer match mode.
- Complex fuzzy matching, OCR jitter debounce, and hysteresis are deferred because they can create accidental mute triggers.
- Speaker match results are printed for manual debugging only and are not connected to `MuteCoordinator`.
- The behavior does not add GUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, input automation, or automatic audio control.

## 2026-05-11: v0.6 OCR-driven Detection Dry Run

Decision: add an explicit dry-run loop that repeatedly runs OCR plus speaker matching and prints state changes without controlling audio.

Reasoning:

- v0.6 needs to observe OCR and speaker matching stability over repeated iterations before any automatic audio behavior is considered.
- The loop can OCR a fixed image or capture a target window before OCR, but it remains an explicit `--detect-loop` debug mode.
- Dry-run output includes raw OCR text, normalized text, matched/not matched, matched speaker, and matched-state changes.
- Dry-run results are not passed to `MuteCoordinator` and do not create or call real audio services.
- Contains matching remains debug-only; before audio integration the project must add debounce/hysteresis or stricter speaker-label parsing to reduce false positives from OCR noise.
- The behavior does not add GUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, input automation, or automatic audio control.

## 2026-05-11: v0.7 Detection Stability Gate

Decision: add a stability gate to the OCR-driven dry-run loop before any audio integration.

Reasoning:

- Raw OCR speaker matching can fluctuate frame to frame, and contains matching remains only a debug raw signal.
- Stable target-present state requires consecutive raw matches, and stable target-absent state requires consecutive raw misses.
- Match and miss thresholds are configurable for manual observation, while conservative defaults avoid reacting to a single noisy frame.
- Dry-run output now reports both raw match result and stable match state so OCR/matching stability can be evaluated before audio control.
- Stable detection results remain observation-only and are not passed to `MuteCoordinator`, `IAudioMuteService`, or `WindowsAudioMuteService`.
- The behavior does not add fuzzy matching, GUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, input automation, or automatic audio control.

## 2026-05-15: v0.8 Simulated Audio Integration

Decision: add an explicit simulated detection audio mode that uses stable detection results to request `LoggingAudioMuteService` actions before any real audio integration.

Reasoning:

- Raw contains matching remains too broad to drive audio actions directly.
- v0.8 validates that stable matched/not-matched states can drive mute/restore sequencing without touching system audio.
- The mode prints raw match, stable match, and simulated audio action for manual inspection.
- `WindowsAudioMuteService` is not created in this mode, and `--real-audio` is rejected when combined with simulated detection audio.
- Real Windows audio integration from OCR remains a later phase after simulated sequencing is reviewed.
- The behavior does not add fuzzy matching, GUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, input automation, or real audio control.

## 2026-05-15: v0.9 Guarded Real Audio Integration

Decision: allow stable detection to drive real Windows audio only behind multiple explicit opt-in flags.

Reasoning:

- Real detection audio requires `--detect-loop`, `--real-audio`, and `--allow-real-audio-from-detection`; the target process may come from CLI `--process <target>` or from config `TargetProcessName`.
- Raw contains matching still must not directly drive audio; only stability-gated matched/not-matched state may request mute/reduce/restore.
- The implementation reuses `WindowsAudioMuteService` and existing audio filter modes instead of adding a new audio stack.
- The mode prints a clear warning and target process/audio settings before starting.
- Manual verification should start with a low-risk process such as Chrome before trying a game process.
- Shutdown and cancellation should attempt restore where possible.
- The behavior does not add GUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, input automation, game file modification, or keyboard/mouse automation.

## 2026-05-15: v0.9.1 Restore After Partial Audio Apply Failure

Decision: track that restore may be needed as soon as detection-driven audio apply starts, before `MuteAsync` finishes successfully.

Reasoning:

- Windows audio apply can snapshot and modify sessions one at a time.
- If one session is already muted or reduced and a later session fails, shutdown must still attempt restore.
- `DetectionAudioCoordinator` does not report the filter as successfully active until `MuteAsync` succeeds, but it keeps enough state to retry restore after partial failure.
- Restore state is cleared only after `RestoreAsync` succeeds; if restore fails, a later shutdown/cancellation restore can retry.
- Automated coverage uses fake `IAudioMuteService` implementations and does not control real system audio.

## 2026-05-15: v0.10 Manual OCR Region Calibration

Decision: add an explicit manual OCR region calibration mode that captures one target-window screenshot, opens a minimal WinForms selection window, and saves both pixel and ratio OCR regions to local JSON.

Reasoning:

- Manual calibration makes the speaker-name OCR area reviewable before relying on repeated OCR or audio behavior.
- A minimal WinForms window is sufficient for drag-selecting a rectangle and avoids turning the console prototype into a full GUI application.
- Saving both pixel and ratio coordinates lets later runs reuse exact coordinates or adapt the region to different screenshot sizes.
- Calibration mode does not run OCR, call `MuteCoordinator`, create `WindowsAudioMuteService`, or control real system audio.
- The behavior does not add WPF, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-15: v0.11 OCR Region Source Resolution

Decision: add a unified OCR region source resolver that supports absolute pixel regions, local calibration JSON, and named presets.

Reasoning:

- One resolver keeps one-shot OCR, detection dry-run, simulated detection audio, and guarded real detection audio consistent.
- `--ocr-region` remains the direct absolute pixel override for quick debugging.
- `--ocr-region-config` loads the local calibration file from v0.10 and uses `regionRatio` to compute a pixel region for the current image size.
- Preset names are limited to `auto`, `2560x1600`, `1920x1080`, and `none`.
- Built-in presets for `2560x1600` and `1920x1080` must be backed by real calibration data; no guessed coordinates are added.
- Manual calibration remains the fallback when resolution differs or preset data is unavailable.
- Guarded real audio detection must have a valid OCR region source so full-window OCR does not drive real audio.
- The behavior does not add automatic UI定位, OpenCV, ONNX, WPF, WinUI, overlay, masking, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-15: v0.12 Configuration Integration

Decision: extend local JSON configuration to cover common OCR, detection loop, stability threshold, audio filter, and OCR region source defaults while keeping guarded real audio behind explicit CLI safety flags.

Reasoning:

- Long OCR and detection commands are error-prone, especially once `--ocr-region-config`, Tesseract path, language, loop timing, and stability thresholds are all needed together.
- `AppSettings` remains the source for safe defaults, while CLI arguments continue to override config values for quick manual testing.
- `config.example.json` remains safe with `RealAudioEnabled=false`, and `config.local.json` is ignored so users can keep local paths out of the repository.
- Runtime real audio is not enabled by config alone in v0.12; `--real-audio` and `--allow-real-audio-from-detection` remain explicit CLI safety gates.
- Existing OCR region source behavior is preserved, including ambiguity rejection for competing region sources.
- The behavior does not add a GUI settings editor, automatic region detection, OpenCV, ONNX, WPF, WinUI, overlay, masking, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-15: v0.13 Usability Hardening

Decision: add explicit configuration validation, effective configuration output, and runtime preflight checks before starting OCR, capture, detection, or audio work.

Reasoning:

- Long local configs are easier to diagnose when users can run `--validate-config` without starting OCR, detection, capture, or audio.
- `--print-effective-config` makes CLI-over-config merge behavior visible and shows the real-audio safety gate state.
- Preflight checks catch common missing Tesseract, missing image, missing OCR region config, and missing target process problems before deeper runtime errors.
- Error categories distinguish configuration, OCR preflight, capture preflight, and audio safety problems so users know which setting to fix.
- Real audio safety rules are unchanged: config alone cannot enable runtime real audio or guarded detection audio.
- The behavior does not add dependencies, GUI settings editor, automatic region detection, OpenCV, ONNX, WPF, WinUI, overlay, masking, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.
