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

## 2026-05-16: v0.14 Minimal WinForms Control Panel

Decision: add an explicit `--gui` launch mode that opens a minimal WinForms control panel as a thin local UI over existing command services.

Reasoning:

- The core OCR, detection, calibration, configuration, and audio safety paths are already command-line verifiable, but daily use requires fewer long commands.
- WinForms is already available through the Windows desktop target and the existing calibration window, so no new GUI dependency is needed.
- The control panel should orchestrate existing services instead of duplicating OCR, detection, or audio logic in form event handlers.
- Default console startup remains unchanged when `--gui` is not supplied.
- The first panel exposes config validation, effective config output, calibration, one-shot OCR, dry-run detection, and simulated detection audio.
- Guarded real audio remains disabled by default and is not exposed without explicit warning and confirmation.
- Stop/close should cancel running detection and allow simulated audio restore paths to run.
- The behavior does not add WPF, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-17: v0.15 GUI Hardening

Decision: harden the existing minimal WinForms panel instead of adding new GUI features or enabling real audio from the UI.

Reasoning:

- The first panel is useful, but daily use needs clearer status, safer button states, and better cancellation feedback before the GUI grows.
- A small UI run-state controller keeps Idle, Running, Stopping, and Error transitions testable without launching WinForms.
- Browse and button failures should go to the GUI log area instead of surfacing as unhandled WinForms dialogs.
- Stop/Close behavior should request cancellation and let existing detection cleanup and simulated restore paths run.
- Guarded real audio remains outside the GUI main actions; real audio safety gates are unchanged.
- The behavior does not add new dependencies, WPF, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-17: v0.16 GUI Guarded Real Audio Page

Decision: expose guarded real audio in the minimal WinForms panel only behind explicit enablement, warning, confirmation, and existing detection/audio safety rules.

Reasoning:

- CLI guarded real audio is already manually verified, but GUI users need a local control path that does not require long commands.
- The GUI must still be a thin wrapper over existing config, preflight, detection, stability, and audio services.
- `WindowsAudioMuteService` is created only after the user explicitly enables guarded real audio and confirms the warning dialog.
- Stable detection state drives real audio actions; raw OCR/speaker matches never directly control real audio.
- The guarded real audio UI requires a valid target process, valid config/preflight, and valid OCR region source before starting.
- GUI detection loops use live target-process capture by default; fixed-image detection is an explicit dry-run/simulated debug option only.
- Guarded real audio rejects fixed-image detection so a stale screenshot cannot drive real audio.
- GUI-started detection loops default to running until Stop; an optional GUI loop count can override config for short manual tests without changing CLI/config behavior.
- GUI detection tuning defaults are biased toward live dialogue responsiveness: loop interval 200 ms, capture delay 100 ms, match threshold 2, and miss threshold 1.
- GUI tuning overrides apply only to the current GUI run and are not written back to local JSON config.
- The main GUI preserves its bounds around OCR region calibration so the calibration form does not leave the control panel resized.
- WinForms GUI and calibration windows use explicit DPI/scaling settings to avoid repeated autoscale/layout shrink after calibration closes.
- OCR speaker matching now distinguishes strong matches, weak near-matches, clear misses, and unknown/noisy misses so real audio is not restored from a single OCR jitter frame.
- Weak near-matches for the current stable speaker and short/empty OCR noise can hold the stable matched state, while clear non-target text remains eligible for fast restore through the miss threshold.
- Real audio is still driven only by stability-gated state, not by raw OCR match frames.
- Live detection now reports per-iteration timing for capture, OCR, match, audio action, and total elapsed time so responsiveness problems can be diagnosed without changing safety gates.
- Live target-window capture uses a reusable capture session where available: it activates/acquires the target window once at loop startup, reuses the cached window handle for subsequent frames, and reacquires only if the handle is invalid, minimized, or capture fails.
- Realtime detection disables debug image writes by default so each loop does not save `debug-captures/capture-latest.png` and `debug-ocr/ocr-input-latest.png`; Tesseract still receives a local temporary OCR input file when a cropped region is required.
- Debug image saving remains available through configuration or GUI tuning for troubleshooting, but it is treated as diagnostic output because it can dominate capture/OCR latency.
- Realtime live detection now prefers region-only capture: configured OCR region sources are resolved against the current target window size, and only that small rectangle is captured for OCR. Full-window capture remains a fallback when region-only capture cannot be resolved or applied.
- Stop/Close should reuse existing cancellation and restore paths if real audio may have been applied.
- Default console and default GUI launch remain safe and do not control real system audio.
- The behavior does not add new dependencies, WPF, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-19: v0.17 WPF Modern GUI Shell

Decision: introduce a modern WPF shell for `--gui` while keeping existing core services and CLI behavior unchanged.

Reasoning:

- The WinForms control panel proved the workflow but remained visually cramped and hard to evolve into a long-term desktop tool.
- WPF is already available through the Windows desktop stack and gives the project a cleaner path for a BetterGI-inspired shell without third-party UI dependencies.
- The shell separates Overview, Config, OCR, Detection, Audio, and Logs with left navigation, a top status bar, card-style panels, and a distinct Guarded Real Audio Danger Zone.
- Light and dark palettes are loaded at startup from the Windows app theme, with an optional DWM backdrop hint used only as a cosmetic enhancement.
- Existing `GuiCommandService` remains the bridge to config, OCR, calibration, detection, simulated audio, and guarded real audio flows, so business logic is not duplicated in WPF code-behind.
- Guarded real audio remains visually separated as a danger zone and still requires explicit checkbox enablement, preflight, stable detection, and confirmation.
- The existing WinForms OCR region calibration selector can remain because replacing it is outside this shell milestone.
- Capture and calibration continue to use visible screen pixels, so the target window must be restored, visible, and uncovered. If automatic restore/activation fails, WPF calibration provides a manual foreground fallback: the WPF window is minimized, the user manually switches to the target window, and the app captures the current foreground window only after validating that its process matches the configured target.
- The same manual foreground fallback is available for WPF live detection startup. Dry-run detection, simulated detection audio, and guarded real audio can initialize a live capture session from the validated foreground target window when automatic restore fails. After a foreground detection session starts, WPF stays minimized until the operation stops or fails so it does not immediately steal focus and cause the target to minimize again.
- Foreground detection sessions use the validated foreground window handle directly and do not silently fall back to process-name restore/reacquire on the first frame. If that foreground handle later becomes invalid or minimized, detection reports a clear visible-pixel capture error instead of simulating focus changes or keyboard/mouse automation.
- Successful calibration writes a local `debug-ocr/calibration-region-latest.png` preview cropped from the selected rectangle. This debug artifact is for diagnosing coordinate, scaling, and wrong-region problems when OCR output is empty after calibration.
- The project still does not implement background window capture, DirectX capture, hooks, injection, input automation, simulated Alt+Tab, or game memory access.
- This milestone intentionally does not add a GUI config editor or save edited configuration.
- Real audio safety gates, CLI behavior, OCR/detection/audio services, and MuteCoordinator behavior are unchanged.
- The behavior does not add third-party UI dependencies, WinUI, overlay, masking, OpenCV, ONNX, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-20: v0.18 OCR Backend Replacement / Low-latency OCR Spike

Decision: add an optional in-process PaddleOCRSharp backend behind the existing `IOcrService` abstraction while keeping `TesseractCli` as the default fallback.

Reasoning:

- Region-only live capture reduced screenshot latency to tens of milliseconds, but realtime logs still show Tesseract CLI OCR taking roughly 1-2 seconds per iteration.
- The main remaining latency is the per-iteration external `tesseract.exe` startup and OCR work, not capture.
- `PaddleOcrLocal` is introduced as a local, in-process backend candidate so the OCR engine and model can be initialized once and reused across detection-loop iterations.
- Existing `TesseractCli` behavior remains available and is still the safe default until Paddle has enough manual validation.
- OCR backend selection is config/CLI driven through `Ocr.Engine`; detection and audio behavior continue to use `IOcrService`.
- Paddle runtime/model path errors should be clear and actionable, including missing native DLL, missing model directory, or unsupported architecture suggestions.
- OCR timing logs remain the source of truth for comparing backend latency on the same OCR region.
- The WPF/CLI detection path logs the selected OCR engine at loop startup so users can confirm whether a run is actually using `TesseractCli` or `PaddleOcrLocal`.
- The WPF shell exposes OCR engine selection for the current run and a warm-up action so Paddle initialization can happen before the first detection frame.
- The GUI command layer reuses the selected OCR service across WPF Start/Stop cycles; selecting Paddle must not silently fall back to Tesseract.
- `--ocr-benchmark` runs repeated OCR on the same prepared input and reports per-run raw text and elapsed time. This isolates OCR accuracy and latency from live capture.
- Benchmark output separates first-run timing from warm-run average because Paddle's first initialization can be much slower than steady-state OCR.
- Detection can optionally save OCR failure samples under `debug-ocr/failures` with sidecar metadata. This is meant to diagnose why names such as `流浪者` are misrecognized instead of hiding the issue with extra state-machine thresholds.
- Optional preprocessing settings are explicit and disabled by default: scale, padding, grayscale, invert, and threshold. The default remains raw crop because earlier manual checks showed preprocessing can make small Chinese text worse.
- Real audio safety gates, stable detection requirements, MuteCoordinator behavior, capture rules, and WPF guarded real audio confirmation are unchanged.
- The behavior does not add overlay, masking, game memory access, hooks, injection, game file modification, or keyboard/mouse automation.

## 2026-05-21: v0.18.1 OCR Backend Diagnostic Stabilization

Decision: key WPF OCR backend warm/status state by the backend runtime identity instead of by `OcrEngine` alone.

Reasoning:

- Paddle warm status is only meaningful for the exact model/runtime settings that were initialized.
- If `PaddleModelDirectory` or `PaddleRuntimeDirectory` changes, showing the previous Paddle instance as `Ready` is misleading because the next OCR call may recreate and reinitialize the backend.
- The GUI backend cache now uses a key containing `OcrEngine`, `PaddleModelDirectory`, and `PaddleRuntimeDirectory`, while still allowing a previously warmed key to be reused when switching back.
- Real audio safety gates, OCR recognition behavior, detection stability, and audio coordination are unchanged.
