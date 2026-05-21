# AGENTS.md

## Project overview

This project is a Windows desktop utility written in C#.

The goal is to detect when a configured game character is speaking and temporarily mute or reduce the game process audio. Later versions may support OCR-based speaker detection and optional screen-region masking.

The project should behave like a local accessibility/preferences tool.

The agent should prioritize:

- correctness;
- safety;
- reversibility;
- minimal changes;
- small testable modules;
- command-line verifiable results;
- clear separation between prototype code and long-term architecture.

## Current milestone

The current milestone is **v0.19.2 Foreground UX / Resume Flow**.

The previous **v0.1 Audio MVP**, **v0.2 Local JSON Configuration**, **v0.3 Window Capture Prototype**, **v0.4 OCR Text Extraction Prototype**, **v0.5 Speaker Detection from OCR Text Prototype**, **v0.6 OCR-driven Detection Dry Run**, **v0.7 Detection Stability Gate**, **v0.8 Simulated Audio Integration**, **v0.9 Guarded Real Audio Integration**, **v0.9.1 Partial Audio Apply Restore Fix**, **v0.10 Manual OCR Region Calibration**, **v0.11 OCR Region Source Resolution**, **v0.12 Configuration Integration**, **v0.13 Usability Hardening**, **v0.14 Minimal WinForms Control Panel**, **v0.15 GUI Hardening**, **v0.16 GUI Guarded Real Audio Page**, **v0.17 WPF Modern GUI Shell**, **v0.18 OCR Backend Replacement / Low-latency OCR Spike**, **v0.18.1 OCR Backend Diagnostic Stabilization**, **v0.19 WPF Persistent Control Dock / Interaction Layout**, and **v0.19.1 CaptureLost UI Recovery** are considered implemented and manually verified where applicable:

- simulated speaker input works;
- mute coordination works;
- real Windows audio mode is opt-in only;
- target-process mute mode works;
- target-process reduce-volume mode works;
- shutdown restore behavior works in the tested manual path;
- local JSON configuration works;
- CLI overrides work;
- default execution remains safe;
- `--capture-once` can explicitly trigger screenshot capture;
- target windows can be found by process name where practical;
- debug screenshots can be saved locally;
- capture can attempt foreground activation and wait for capture delay;
- Notepad manual screenshot verification succeeded;
- `--ocr-once` can explicitly invoke Tesseract CLI OCR;
- `--ocr-input` can OCR from an existing image file;
- `--ocr-region` can crop the target OCR area before invoking OCR;
- `debug-ocr/ocr-input-latest.png` can save the actual cropped image passed to OCR;
- raw cropped image OCR works better than scale/grayscale/threshold preprocessing for the current manual sample;
- `SpeakerMatcher` is implemented independently;
- `--detect-speaker-once` works;
- `--speaker-text "流浪者："` and `--speaker-text "流浪者:"` both match;
- OCR + speaker detection debug path works;
- contains matching is documented as debug-only and must not directly drive audio control;
- `--detect-loop` can run OCR-driven dry-run detection;
- fixed-image OCR dry-run works;
- window-capture OCR dry-run works;
- dry-run output includes OCR raw text, normalized text, matched/not matched, matched speaker, and state changed;
- dry-run does not call `MuteCoordinator`, does not create a real audio service, and does not control real system audio;
- `DetectionStabilityGate` is implemented;
- dry-run output includes raw match and stable match state;
- `NotMatched -> NotMatched` does not repeatedly emit stable state changed;
- stable detection can drive simulated audio;
- raw match below threshold does not trigger audio;
- stable matched triggers one simulated mute;
- repeated stable matched does not repeatedly mute;
- shutdown requests simulated restore;
- `--simulate-audio-from-detection` and `--real-audio` conflict is rejected;
- stable detection can drive real audio only behind `--detect-loop`, `--real-audio`, and `--allow-real-audio-from-detection`, with target process supplied by CLI or config;
- guarded real audio mute and reduce-volume modes were manually verified with Chrome;
- partial audio apply failure no longer skips shutdown/cancellation restore;
- default execution still does not screenshot or control real audio;
- OCR output is not connected to `MuteCoordinator` or automatic audio control;
- `--calibrate-ocr-region` can capture a target window and save `ocr-region.json`;
- calibration output includes source image size, pixel region, and ratio region;
- calibration mode does not run OCR, control real audio, or call `MuteCoordinator`;
- Notepad manual calibration verification succeeded;
- `--ocr-region-config` can load `ocr-region.json`;
- `regionRatio` can be converted into current-image pixel OCR regions;
- `--ocr-region-preset` supports `auto`, `2560x1600`, `1920x1080`, and `none`;
- built-in presets do not use fabricated coordinates when real calibration data is absent;
- guarded real audio safety gates remain unchanged;
- OCR / Detection / AudioFilter common parameters are integrated into local JSON configuration;
- `config.local.json` is gitignored;
- CLI values override config values;
- config alone cannot enable runtime real audio or guarded detection audio;
- `--config config.local.json --ocr-once` and `--config config.local.json --detect-loop` have been manually verified;
- `--validate-config` and `--print-effective-config` are available for diagnostics;
- preflight checks are available for common OCR, image, OCR region config, process, and guarded real-audio region safety problems;
- `--gui` can explicitly launch a minimal WinForms control panel;
- default console behavior remains safe when `--gui` is not supplied;
- the GUI can validate config, print effective config, calibrate OCR region, test OCR once, run dry-run detection, run simulated detection audio, and stop running detection;
- the GUI OCR input default points to `debug-captures/capture-latest.png`;
- `OcrInputPreparer` rejects using `debug-ocr/ocr-input-latest.png` as OCR input;
- GUI state management supports `Idle`, `Running`, `Stopping`, and `Error`;
- GUI button states are hardened while operations are running;
- GUI Stop behavior is hardened;
- Browse exceptions are logged to the GUI log area;
- GUI logs auto-scroll;
- Test OCR Once, Dry-run Detection, and Simulated Detection Audio have been manually verified;
- Stop logs `Stop requested` and `Operation cancelled`;
- the GUI does not create `WindowsAudioMuteService` by default;
- the GUI does not control real system audio by default;
- GUI guarded real audio is available behind explicit checkbox and confirmation;
- GUI guarded real audio reuses stable detection and existing guarded real audio paths;
- fixed-image mode no longer drives detection loops by default;
- guarded real audio rejects fixed-image mode;
- GUI tuning parameters are available;
- `SaveDebugImages` is disabled by default for live detection;
- live detection can use region-only capture;
- timing logs show capture latency has been reduced substantially, and Tesseract CLI OCR is now the main observed bottleneck;
- WPF `--gui` launches the modern WPF shell while default console behavior remains safe;
- the WPF shell keeps existing config, OCR, detection, simulated audio, guarded real audio, Stop, and log flows accessible;
- manual foreground fallback exists for calibration and detection launch paths where visible-pixel capture cannot automatically restore the target window;
- PaddleOcrLocal backend is available behind the OCR service abstraction;
- WPF OCR engine selection and Paddle warm-up are available;
- Paddle OCR service cache is keyed by engine plus runtime/model settings;
- WPF guarded real audio paths can use PaddleOcrLocal;
- Paddle warm-run OCR latency is substantially lower than Tesseract CLI in manual testing;
- Tesseract CLI remains available as fallback;
- v0.19 persistent control dock is implemented and keeps guarded real-audio controls visible across pages;
- v0.19.1 treats foreground capture lost as a recoverable UI state and keeps the WPF UI usable after capture loss;
- v0.19.2 focuses on foreground activation, optional input-based foreground fallback, and Resume/Reconnect without changing real audio safety gates;
- real audio safety gates remain unchanged;
- default target speakers are `流浪者` and `Wanderer`.

Scope for v0.19.2:

- Console app remains supported.
- GUI remains explicit via `--gui`.
- Keep existing CLI behavior.
- Keep the existing WPF shell, left navigation, and persistent control/status dock.
- Focus on foreground UX and Resume/Reconnect after CaptureLost.
- Add best-effort Win32 target window activation before manual fallback.
- Calibration startup may attempt best-effort target foreground activation.
- Dry-run, simulated detection audio, and guarded real audio startup may attempt best-effort target foreground activation.
- Optional explicit `SendInput` / `Alt+Tab` foreground fallback is allowed only when user-visible/configurable and only for bringing the target window to foreground.
- If activation fails, fall back to the existing manual foreground flow.
- CaptureLost should support Resume/Reconnect where practical, or clearly document the limitation if deferred.
- Keep Paddle OCR backend behavior.
- Keep foreground-region-only capture behavior.
- Keep existing GUI functions working.
- Existing simulated audio GUI mode must continue working.
- Existing guarded real audio GUI mode must continue working behind explicit checkbox and confirmation.
- Default GUI launch must not start real audio.
- Real audio safety gates remain unchanged.
- Global hotkeys remain deferred to v0.20 and are not part of v0.19.2.
- Windows.Graphics.Capture / DirectX capture backend is not prohibited, but it is not implemented in v0.19.2 and belongs to a separate future capture backend spike.
- The persistent control dock must show:
  - run state: `Idle`, `Starting`, `Detecting`, `Reduced`, `Restored`, `Stopping`, or `Error`;
  - target process;
  - target speakers;
  - OCR engine;
  - OCR backend warm/status;
  - last OCR text;
  - last detected speaker;
  - last audio action;
  - current audio state.
- The persistent control dock must expose common guarded real-audio controls:
  - Start Guarded Real Audio;
  - Stop;
  - Restore, if available and applicable.
- Start Guarded Real Audio must still require:
  - an explicit enable checkbox or equivalent armed state;
  - confirmation dialog;
  - preflight;
  - valid OCR region;
  - stable detection only.
- Resume/Reconnect must not bypass guarded real audio checkbox, confirmation, preflight, valid OCR region, or stable detection requirements.
- Stop and Restore must not bypass safety or cleanup behavior.
- Page responsibilities:
  - Overview: high-level summary cards and no excessive duplicate controls;
  - OCR: OCR engine selection, warm-up, calibration, Test OCR Once, preprocessing, and failure sample options;
  - Detection: loop interval, capture delay, match threshold, miss threshold, run until stop, and timing summary;
  - Audio: audio mode, volume percent, safety explanations, and guarded real audio readiness details;
  - Logs: full log viewer, copy, clear, and auto-scroll.
- Keep CLI behavior working.
- Default launch without `--gui` remains existing console behavior.
- Real audio safety gates must remain conceptually unchanged.
- Default run must remain safe.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- Existing v0.7 stability-gate behavior must remain stable.
- Existing v0.8 simulated audio behavior must remain stable.
- Existing v0.9 guarded real audio behavior must remain stable.
- Existing v0.10 calibration behavior must remain stable.
- Existing v0.11 OCR region source behavior must remain stable.
- Existing v0.12 configuration integration behavior must remain stable.
- Existing v0.13 usability hardening behavior must remain stable.
- Existing v0.14 minimal WinForms control panel behavior must remain stable.
- Existing v0.15 GUI hardening behavior must remain stable.
- Existing v0.16 GUI guarded real audio behavior must remain stable.
- Existing v0.17 WPF modern GUI shell behavior must remain stable.
- Existing v0.18 OCR backend replacement behavior must remain stable.
- Existing v0.18.1 OCR backend cache-key behavior must remain stable.
- Existing v0.19 persistent control dock behavior must remain stable.
- Existing v0.19.1 CaptureLost UI recovery behavior must remain stable.
- .NET 8.
- Windows x64.
- VS Code / Codex / Visual Studio friendly workflow.

Out of scope for the current milestone:

- GUI config editor.
- Saving edited config.
- Global hotkeys for v0.19.2; they remain deferred to v0.20.
- Tray icon for v0.19.2.
- Always-on-top mini status window for v0.19.2.
- Changing OCR backend architecture.
- Removing Tesseract CLI fallback.
- New feature work outside foreground UX / Resume Flow.
- New major features.
- Real audio enabled by default.
- Bypassing `--real-audio` / `--allow-real-audio-from-detection` semantics conceptually.
- Changing real audio safety gates.
- Changing `MuteCoordinator`.
- New calibration UI features.
- Automatic region detection.
- Fabricated preset coordinates.
- WinUI.
- Overlay masking.
- Face detection.
- ONNX.
- OpenCV.
- Automatic real audio without existing guarded real-audio flags.
- Gameplay automation remains out of scope.
- Auto-clicking, auto-dialogue skipping, combat automation, task automation, macro loops, or game-control input automation remain out of scope.
- Limited foreground switching input via `SendInput` / `Alt+Tab` is allowed only when explicitly requested, only for bringing the target window to foreground, and must be user-visible/configurable.
- Windows.Graphics.Capture / DirectX capture backend evaluation is deferred to a separate future capture backend spike.
- Anti-cheat bypass.
- Game memory reading or modification.
- Hooking or injection.

Done when:

- `dotnet build` passes.
- If tests exist, `dotnet test` passes.
- Default launch without `--gui` keeps existing console behavior.
- `--gui` launches the WPF shell.
- Existing WinForms control panel may remain only as temporary fallback.
- The WPF shell keeps Validate Config, Print Effective Config, Calibrate OCR Region, Test OCR Once, Start Dry-run Detection, Start Simulated Detection Audio, Start Guarded Real Audio, and Stop accessible.
- The WPF shell keeps existing left navigation and modern shell behavior.
- A persistent control/status dock is visible across pages.
- The persistent dock shows run state, target process, target speakers, OCR engine, backend warm/status, last OCR text, last detected speaker, last audio action, and current audio state.
- Start Guarded Real Audio, Stop, and Restore where applicable are available from the persistent dock without hiding them only in the Audio page.
- Overview, OCR, Detection, Audio, and Logs pages follow their v0.19 responsibilities without excessive duplicate controls.
- Logs remain readable, large enough, and copyable.
- Calibrate OCR Region attempts best-effort target foreground activation before manual fallback.
- Start Dry-run / Simulated / Guarded Real Audio attempts best-effort target foreground activation before manual fallback.
- Optional `SendInput` / `Alt+Tab` fallback, if implemented, is explicit/user-visible/configurable.
- CaptureLost does not freeze UI.
- Resume/Reconnect is available or clearly documented if deferred.
- Resume does not bypass guarded real audio safety gates.
- Manual fallback remains available.
- Guarded real audio cannot start without an explicit checkbox, visible warning, and confirmation dialog.
- Guarded real audio cannot start without valid config/preflight, a valid OCR region source, and a target process from config or UI.
- GUI real audio uses existing guarded real audio paths and safety rules.
- GUI real audio uses stable detection results and never raw match results directly.
- Unknown, null, or blank matched speakers do not trigger real audio.
- Repeated stable matched state does not spam mute/reduce.
- Repeated stable not-matched state does not spam restore.
- Stop/close attempts restore if real audio action may have been applied.
- Existing simulated audio GUI mode continues working.
- The GUI still delegates to existing shared services or a thin command/application layer instead of duplicating OCR, detection, calibration, or audio logic.
- UI updates remain Dispatcher-safe and do not block the UI thread.
- Paddle warm-up does not freeze the UI.
- Existing CLI tests keep passing.
- Tests cover UI-independent command/application services where practical.
- Default run remains safe and does not control real system audio.
- No new dependencies are introduced.
- No hook/injection/game memory/gameplay automation is introduced.
- No third-party UI dependencies, WinUI, OpenCV, ONNX, masking, overlay, game-control automation, game memory access, hooking, or injection are introduced.
- The final response reports changed files, verification commands, assumptions, and limitations.

Do not implement later roadmap phases until this milestone works.

## Tech stack

- Language: C#
- Runtime: .NET 8 or newer
- Target OS: Windows
- Target architecture: Windows x64
- Initial app type: console app
- GUI option for current milestone: WPF shell with foreground UX / Resume Flow launched explicitly with `--gui`
- WinForms may remain for the existing calibration selector or temporary fallback only.
- Later GUI options: WinUI only if explicitly requested in a future milestone
- Audio control: NAudio or Windows Core Audio APIs
- Configuration: local JSON using .NET built-in JSON support unless a stronger reason is documented
- Image processing later: TBD
- OCR later: TBD
- Packaging later: TBD

Do not assume administrator privileges unless explicitly required and explained.

Do not introduce a GUI config editor, config saving, global hotkeys, tray icon, always-on-top mini window, overlay, gameplay automation, new OCR backend architecture, image-processing dependency, or model-inference dependency during the v0.19.2 Foreground UX / Resume Flow milestone. Limited foreground switching input via `SendInput` / `Alt+Tab` is allowed only when explicitly requested and must not send gameplay commands. WPF is allowed only as a thin shell over existing services. Avoid OpenCV, ONNX, WinUI, and third-party UI dependencies for this milestone.

## Tooling workflow

Codex Windows desktop app, Visual Studio, and VS Code are all available.

Use this division of responsibility:

- Codex Windows desktop app:
  - primary agent workspace;
  - focused code changes;
  - reviewable diffs;
  - Git worktrees;
  - small implementation tasks;
  - code review tasks;
  - command-line verification.

- Visual Studio:
  - debugging;
  - breakpoint inspection;
  - WPF GUI diagnostics;
  - WinUI work later if explicitly requested;
  - NuGet inspection;
  - Windows-specific diagnostics;
  - audio API / COM debugging;
  - runtime behavior verification.

- VS Code:
  - lightweight editing;
  - markdown files;
  - small manual edits;
  - terminal workflows;
  - quick code review.

Do not rely on Visual Studio-only workflows unless explicitly requested.

All essential build, test, and run steps must work from the `dotnet` CLI.

Visual Studio may be used for debugging, but command-line verification is still required before a task is considered complete.

## Development phases

Follow this order unless the user explicitly changes the roadmap:

1. Simulated speaker input + target process mute/restore.
2. Real Windows audio control behind `IAudioMuteService`.
3. Audio filter modes: mute and reduce volume.
4. Local JSON configuration.
5. Window capture prototype.
6. OCR text extraction from a configurable screen region.
7. Speaker detection from OCR text.
8. OCR-driven detection dry run.
9. Detection stability gate.
10. Simulated audio integration.
11. Guarded real audio integration.
12. Manual OCR region calibration.
13. OCR region source resolution.
14. Configuration integration.
15. Minimal WinForms control panel.
16. GUI hardening.
17. GUI guarded real audio page.
18. WPF modern GUI shell.
19. OCR backend replacement / low-latency OCR spike.
20. v0.19 WPF persistent control dock / interaction layout.
21. v0.19.1 CaptureLost UI recovery.
22. v0.19.2 Foreground UX / Resume Flow.
23. v0.20 Global hotkey / tray / status mini-window.
24. Future capture backend spike for Windows.Graphics.Capture / DirectX capture evaluation.
25. Stable mute/unmute coordination with debounce and recovery.
26. Optional masking.

Do not implement later-phase functionality prematurely.

For every task, identify which phase is being worked on and avoid touching unrelated phases.

## Core modules

Use these module boundaries unless the user asks for a different design:

- `IGameWindowCapture`: captures frames from the target game window.
- `IOcrService`: extracts text from a specific screen region.
- `ISpeakerDetector`: determines the current speaker from OCR text or simulated input.
- `IAudioMuteService`: mutes, reduces, and restores the target game process or audio session according to configured audio filtering behavior.
- `MuteCoordinator`: coordinates speaker detection, mute/filter state, debounce logic, and recovery.
- `AppSettings`: stores target characters, target process name, OCR region, timing thresholds, audio settings, and feature toggles.
- `AppSettingsLoader`: loads and validates local JSON configuration.
- `AudioFilterOptions`: stores mute/reduce-volume behavior and validation rules.
- `WindowCaptureOptions`: stores target window or screen-region capture settings for debug screenshots.
- `OcrRegionCalibrationOptions`: stores explicit calibration input/output settings.
- `OcrRegionCalibrationResult`: stores the selected pixel and ratio regions.
- `OcrRegionCalibrationFile`: serializes/deserializes local calibration JSON.
- `WindowsOcrRegionCalibrator`: captures and displays a screenshot for manual OCR region selection.
- A small calibration form: displays the screenshot and supports drag-selecting a rectangle.
- `OcrRegionSourceResolver`: resolves the effective OCR region from pixels, calibration JSON, or presets.
- `OcrRegionSourceOptions`: stores explicit OCR region source choices.
- `OcrRegionPreset` / `OcrRegionPresetRegistry`: represent supported preset names and real calibration-backed preset data.
- `OcrSettings` or equivalent: stores OCR provider, Tesseract, language, page segmentation, and OCR region source defaults.
- `DetectionSettings` or equivalent: stores loop timing and stability threshold defaults.
- `Gui/MainForm`, WPF shell, or equivalent: hosts the explicit GUI control panel.
- `UiLogSink` or equivalent: forwards logs and command output to the UI.
- A thin application or command service: lets the UI call existing config, OCR, detection, calibration, and audio flows without duplicating logic in the form.
- `GuiRunState` or equivalent: represents UI state such as Idle, Running, Stopping, and Error if useful.
- `GuiRealAudioConfirmationState` or equivalent: represents UI confirmation and enablement state for guarded real audio if useful.
- `GuiRuntimeStatus`, `GuiStatusSnapshot`, `GuiAudioState`, or equivalent: represents persistent WPF control/status dock state without coupling core services to WPF controls.
- Guarded real audio UI command/service: lets the GUI reuse existing guarded real audio paths without duplicating detection or audio logic.
- WPF modern shell: presents the existing config, OCR, detection, simulated audio, guarded real audio, and logging flows without duplicating core behavior.
- Theme service or equivalent: detects startup light/dark theme and applies readable palettes if needed.

For v0.19.2, expected work is limited to:

- preserving the v0.19 WPF persistent control/status dock and left navigation behavior;
- preserving the v0.19.1 CaptureLost UI recovery behavior;
- adding best-effort Win32 foreground activation before manual fallback;
- optionally adding explicit, user-visible/configurable `SendInput` / `Alt+Tab` foreground fallback for target-window foreground switching only;
- adding or hardening Resume/Reconnect after CaptureLost where practical;
- applying foreground activation to calibration startup and dry-run/simulated/guarded real-audio detection startup;
- falling back to manual foreground flow when activation fails;
- using existing core services and `GuiCommandService` or an equivalent shared application service;
- preserving CLI behavior and existing command-line tests;
- preserving config and CLI merge behavior;
- preserving Paddle OCR backend behavior and Tesseract CLI fallback;
- preserving foreground-region-only capture behavior;
- preserving guarded real audio safety gates;
- preserving existing simulated audio GUI mode;
- preserving existing guarded real audio GUI mode;
- adding tests for UI-independent foreground UX, Resume/Reconnect, or runtime status helpers where practical.

Do not create a GUI config editor, save edited config, add global hotkeys, add a tray icon, add an always-on-top mini status window, enable real audio by default, change OCR backend architecture, remove Tesseract CLI fallback, create new calibration UI behavior beyond foreground activation and manual fallback, detect regions automatically, fabricate preset coordinates, or weaken existing real-audio guard flags during v0.19.2.

## OCR region source rules

For v0.19.2 Foreground UX / Resume Flow, preserve these rules:

- OCR region source priority:
  1. `--ocr-region` absolute pixels.
  2. `--ocr-region-config` calibration JSON.
  3. `--ocr-region-preset`.
  4. No region, allowed for `--ocr-once` debug only, but not recommended.
- Guarded real audio detection should require a valid OCR region source.
- Calibration files must be local JSON.
- Do not silently create or overwrite calibration files.
- If a calibration file is missing or invalid, give a clear error.
- Prefer `regionRatio` from calibration files for resolution independence.
- Validate computed pixel regions against current image bounds.
- Existing `--ocr-region` pixel mode must continue working.
- Real audio safety gates remain unchanged.
- Built-in presets must not use guessed coordinates.
- If preset coordinates are not configured from real calibration data, return a clear "preset not configured; run calibration" error.
- The project does not aim for full-resolution automatic adaptation.
- Default planned preset names are `2560x1600` and `1920x1080`, but their coordinates must come from real calibration data.
- Other resolutions, or inaccurate presets, should use manual calibration to generate `ocr-region.json`.

## Speaker matching rules

For v0.19 WPF persistent control dock / interaction layout, preserve these rules:

- Trim whitespace.
- Handle newlines around text.
- Ignore common trailing speaker punctuation such as `:` and `：`.
- English matching should be case-insensitive.
- Exact match and simple contains match are allowed.
- Raw contains match must never directly drive real audio.
- Only stable matched state may request real mute/reduce.
- Only stable not-matched state may request real restore.
- Unknown, null, or blank matched speaker must not trigger real mute/reduce.
- Stable state may be computed from raw match results, but real audio must still require explicit guard flags.
- Default stability thresholds should be conservative, such as 2 or 3 consecutive frames.
- Stability thresholds may be configurable by CLI, such as `--match-threshold` and `--miss-threshold`.
- Before auto mute integration, require stricter speaker-label parsing, OCR region confidence, debounce/hysteresis, or an explicit safer match mode.
- Do not add complex fuzzy matching yet.
- Avoid false positives.

## Architecture rules

- UI code must not directly call Windows audio APIs.
- Windows API, COM interop, OCR provider code, screen capture code, and overlay code must be isolated behind service interfaces.
- OCR logic must be replaceable.
- Do not hard-code one OCR provider into core business logic.
- Audio mute/reduce/restore logic must be reversible.
- The application must attempt to restore audio on cancellation, shutdown, and unexpected exceptions.
- Prefer explicit state machines for audio-filter transitions instead of scattered boolean flags.
- Keep core logic testable without launching the game, changing system volume, or requiring OCR.
- Do not put large procedural logic in `Program.cs`.
- Do not put large procedural logic in UI event handlers.
- Do not rewrite unrelated files.
- Do not rename files, classes, public methods, or public APIs unless explicitly requested.
- Prefer the smallest change that solves the current task.
- Do not add abstractions that are not needed for the current phase.
- Do not implement multiple roadmap phases in one task unless explicitly requested.
- Do not silently change existing behavior.

## Mute coordination rules

`MuteCoordinator` should use explicit states when practical, for example:

- `Idle`
- `Muted`
- `Restoring`
- `Faulted`

Required behavior:

- Target speaker starts speaking → apply configured audio filtering to the target process/audio session.
- Target speaker stops speaking → restore audio.
- Non-target speaker speaks → do not apply audio filtering.
- Unknown speaker or detection failure → do not newly apply audio filtering.
- Detection failures must not leave audio permanently filtered.
- Repeated frames or repeated simulated inputs with the same speaker must not trigger repeated audio API calls.
- OCR jitter must later be handled with debounce or hysteresis.
- Restore should be idempotent: calling restore multiple times should be safe.
- Mute/reduce should be idempotent: repeated target detections while already filtered should not spam the audio API or repeatedly reduce volume.
- Shutdown, cancellation, and unexpected exceptions should attempt safe restore.

For v0.19.2:

- The GUI must not duplicate mute, OCR, detection, calibration, or audio logic.
- The GUI must call shared services or a thin command/application layer.
- WPF code-behind must remain UI orchestration only; core services and command paths must remain outside UI event handlers.
- The WPF shell should call `GuiCommandService` or an equivalent shared application service.
- WPF styles, resources, and view models may be introduced when they keep UI state readable and testable.
- Theme resources must maintain readable text/background contrast in light and dark modes.
- Keep Guarded Real Audio visually separated as a danger zone.
- Common guarded real-audio controls should be available from the persistent dock, not hidden only inside the Audio page.
- Runtime status models may be introduced only when they keep the persistent dock WPF-independent and testable.
- UI updates must be Dispatcher-safe.
- Do not block the UI thread.
- Do not make Paddle warm-up freeze the UI.
- The GUI must not create `WindowsAudioMuteService` until the user explicitly starts guarded real audio.
- The GUI must not start guarded real audio without an explicit checkbox, visible warning, and confirmation dialog.
- The GUI must not control real audio by default.
- The GUI must not call `MuteCoordinator` except through existing safe simulated or guarded audio flows.
- Raw OCR/speaker match must never directly drive real audio.
- Only stable matched state may request real mute/reduce.
- Only stable not-matched state may request real restore.
- Unknown, null, or blank matched speakers must not trigger real audio.
- Repeated stable matched state must not spam mute/reduce.
- Repeated stable not-matched state must not spam restore.
- Stop/close should attempt restore if simulated or real audio was active.
- Stop/close must attempt restore through existing coordinator paths if real audio action may have been applied.
- Long-running GUI operations must remain async and cancellable.
- All UI button exceptions should be logged, not shown as unhandled JIT dialogs.
- Core OCR, detection, and audio logic must remain outside `MainForm`.
- Existing guarded real audio detection must require the current explicit guard flags and a valid OCR region source.
- Config alone must not enable guarded real audio detection.
- `--real-audio` and `--allow-real-audio-from-detection` must remain explicit CLI safety gates.
- Existing guarded real audio behavior must remain opt-in only.
- Existing simulated detection audio mode must remain available.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- Existing v0.7 stability-gate behavior must remain stable.
- Existing v0.8 simulated audio behavior must remain stable.
- Existing v0.9 guarded real audio behavior must remain stable.
- Existing v0.10 calibration behavior must remain stable.
- Existing v0.11 OCR region source behavior must remain stable.
- Existing v0.12 configuration integration behavior must remain stable.
- Existing v0.13 usability hardening behavior must remain stable.
- Existing v0.14 minimal WinForms control panel behavior must remain stable.
- Existing v0.15 GUI hardening behavior must remain stable.
- Existing v0.16 GUI guarded real audio behavior must remain stable.
- Existing v0.17 WPF modern GUI shell behavior must remain stable.
- Existing v0.18 OCR backend replacement behavior must remain stable.
- Existing v0.18.1 OCR backend cache-key behavior must remain stable.
- Do not add fuzzy matching yet.

## Safety rules

- Never modify game files.
- Do not inject code into the game process.
- Do not read or modify game memory.
- Do not implement anti-cheat bypass logic.
- Do not implement hook-based gameplay automation.
- Do not implement DirectX hooks unless explicitly discussed and approved later.
- DirectX hooks remain prohibited unless explicitly discussed and approved.
- Windows.Graphics.Capture / DirectX capture backend is not prohibited, but it must be handled as a separate future capture backend spike, not mixed into v0.19.2 foreground UX work.
- Do not implement behavior that automates gameplay decisions.
- Do not add auto-clicking, auto-dialogue skipping, combat automation, task automation, macro loops, or game-control input automation. Limited input simulation for foreground window switching, such as SendInput / Alt+Tab, is allowed only when explicitly requested, must be user-visible/configurable, and must not send gameplay commands.
- Prefer screen capture, OCR, and OS-level audio session control over invasive game modification.
- If a requested feature requires invasive game modification, explain the risk and propose non-invasive alternatives instead.
- Do not store sensitive user data, credentials, cookies, tokens, or game login information.
- Do not send game data or screenshots to external services unless explicitly requested and reviewed.
- Do not add telemetry.

## Configuration rules

Store user configuration in a local JSON file unless the project already uses another configuration format.

For v0.19.2:

- Local JSON configuration is implemented and should now cover common OCR, detection loop, stability threshold, audio filter, and OCR region source defaults.
- The WPF shell may select and validate config files, display effective runtime status, and provide a persistent control dock, but it must not become a persistent settings editor.
- The WPF shell milestone must not add config editing or saving behavior.
- Do not store sensitive information.
- Do not include credentials, cookies, tokens, or game login data.
- Do not make network requests for configuration.
- Do not silently create or overwrite user configuration without explicit request.
- A sample configuration file such as `config.example.json` is allowed.
- `config.example.json` must remain safe with `RealAudioEnabled = false`.
- `config.local.json` should be gitignored for local user overrides.
- Config may provide defaults for OCR, detection, stability, and audio-filter settings.
- CLI arguments must override config values.
- `--real-audio` and `--allow-real-audio-from-detection` must remain explicit CLI safety gates.
- Config must not silently enable guarded real audio detection.
- Do not control real audio unless existing guarded CLI flags are explicitly supplied.
- GUI guarded real audio must also require explicit UI enablement and confirmation.
- Config may provide target process and other defaults, but it must not start real audio by itself.

Configuration should include at least:

- `TargetProcessName`
- `TargetSpeakers`
- `RealAudioEnabled`
- `AudioFilter.Mode`
- `AudioFilter.VolumePercent`
- `Ocr`
- `Detection`

Suggested `Ocr` fields:

- `Engine`
- `TesseractExecutablePath`
- `Language`
- `PageSegmentationMode`
- `RegionConfigPath`
- `RegionPreset`
- optional absolute `Region`

Suggested `Detection` fields:

- `LoopIntervalMs`
- `LoopCount`
- `MatchThreshold`
- `MissThreshold`

v0.4/v0.5/v0.6/v0.7/v0.8/v0.9/v0.10/v0.11/v0.12/v0.13/v0.14/v0.15/v0.16/v0.17/v0.18/v0.18.1/v0.19/v0.19.1/v0.19.2 OCR, speaker debug, dry-run, stability-gate, simulated audio, guarded real audio, calibration, region source, configuration integration, usability hardening, minimal control panel, GUI hardening, GUI guarded real audio, WPF modern shell, OCR backend replacement, OCR backend diagnostic stabilization, persistent control dock, CaptureLost UI recovery, and Foreground UX behavior may include:

- OCR input image path or explicit capture input, only if needed for explicit commands;
- OCR region;
- OCR region source selection;
- OCR region calibration JSON path;
- OCR region preset name;
- OCR debug output directory;
- OCR provider selection, only if a provider is actually introduced;
- speaker matcher options, only if they are needed for explicit debug behavior;
- dry-run timing options;
- match/miss stability thresholds;
- simulated detection audio mode options, only if needed for explicit v0.8 behavior;
- guarded real audio detection options, only if needed for explicit v0.9 behavior, but not the CLI allow safety gate;
- OCR region calibration output path, source screenshot size, pixel region, and ratio region, only if needed for explicit v0.10 calibration behavior;
- OCR region source resolver options.
- minimal UI state needed to start, stop, and display explicit commands, only if it does not duplicate core logic.
- GUI run-state and button-state preferences needed for reliable local control panel behavior.
- GUI guarded real audio confirmation state needed to prevent accidental real audio activation.
- WPF shell theme and navigation state needed to keep the modern control panel readable and extensible.
- WPF runtime status snapshot fields needed by the persistent control dock, only if they do not duplicate core detection or audio logic.

Long-term configuration may later include:

- mute delay/debounce settings;
- restore delay/debounce settings;
- capture region;
- OCR region;
- feature toggles;
- restore behavior;
- logging options.

Default safe values should be:

- `RealAudioEnabled = false`
- `TargetProcessName = "GenshinImpact"`
- `TargetSpeakers = ["流浪者", "Wanderer"]`
- `AudioFilter.Mode = "Mute"`
- `AudioFilter.VolumePercent = 30`

Validate configuration values before using them.

Validation should reject:

- blank target process name;
- null or empty target speaker list;
- null, blank, or duplicate-only target speaker entries;
- unknown audio filter mode;
- `ReduceVolume` mode with `VolumePercent` outside 1 to 100.
- unknown OCR engine;
- blank Tesseract executable path;
- blank OCR language;
- OCR page segmentation mode outside the existing valid range;
- ambiguous OCR region sources;
- unknown OCR region preset;
- invalid detection loop interval;
- invalid loop count;
- match or miss thresholds outside the existing valid range.

Invalid configuration should produce a clear error message.

CLI arguments may override JSON configuration values. Override behavior should be explicit and documented.

## Coding rules

- Use clear C# names.
- Use PascalCase for public members.
- Use `_camelCase` for private fields.
- Keep classes small and focused.
- Add short XML doc comments for public interfaces and important public methods.
- Add Chinese comments for non-obvious logic.
- Do not add long comments for obvious syntax.
- Prefer dependency injection for services that touch the OS, OCR, screen capture, overlay windows, or external libraries.
- Do not introduce new NuGet packages without explaining why.
- Avoid swallowing exceptions silently.
- Use `CancellationToken` for long-running loops or background detection tasks.
- Dispose unmanaged resources and image/audio handles properly.
- Avoid blocking the UI thread.
- Prefer simple, explicit code over clever abstractions.
- Do not create unrelated helper classes.
- Do not move files unless the move directly supports the current task.
- Do not rename output files unnecessarily.
- Keep command examples single-line when practical.

## Dependency rules

Prefer the .NET standard library and existing project dependencies.

Do not add heavy OCR, image-processing, model-inference, overlay, or UI dependencies without explaining:

- why the dependency is needed;
- why existing code is insufficient;
- whether the dependency affects deployment;
- whether it requires native runtime components;
- whether it affects Windows x64 packaging;
- whether there are licensing concerns.

Do not replace the project framework or UI stack without explicit approval.

For v0.19.2:

- Use built-in .NET JSON support where practical.
- Existing NAudio dependency for real Windows audio control may remain.
- Do not add new dependencies for v0.19.2 unless strongly justified.
- Existing minimal WinForms-based calibration window may remain.
- WPF may be used for the GUI shell because the project already targets Windows desktop APIs.
- Existing WinForms code may remain as calibration selector or fallback while WPF shell evolves.
- Use built-in WPF styling, templates, resources, and Windows APIs where practical instead of external UI dependencies.
- Do not add external GUI dependencies.
- Preserve the existing OCR provider abstraction and Paddle/Tesseract selection behavior.
- Do not change OCR backend architecture or add heavy OCR/model dependencies without explicit approval.
- If using Tesseract CLI, do not vendor traineddata files into the repository.
- If using Windows OCR APIs, document package identity or deployment limitations.
- Do not send screenshots to cloud OCR services.
- Do not add OpenCV.
- Do not add ONNX Runtime.
- Do not add third-party WPF control libraries.
- Do not add WinUI dependencies.
- Global hotkey work is deferred to v0.20 and is not prohibited long-term.
- Do not add a global hotkey dependency during v0.19.2.
- Do not add configuration frameworks unless strongly justified.
- Do not add logging frameworks unless explicitly requested.

## Build and run commands

Use these commands unless the project defines more specific ones:

- Restore: `dotnet restore`
- Build: `dotnet build`
- Test: `dotnet test`
- Run: `dotnet run --project <PROJECT_PATH>`

If the project contains a `.sln` file, prefer solution-level commands:

- Build solution: `dotnet build <SOLUTION_NAME>.sln`
- Test solution: `dotnet test <SOLUTION_NAME>.sln`

Visual Studio may be used for debugging, but command-line verification is still required before completion.

Do not claim a command passed unless it was actually run and its output was checked.

If a command cannot be run, state the exact reason.

## Testing instructions

Core mute coordination logic must be testable without actually changing system volume.

Use fake implementations for:

- `IAudioMuteService`
- `ISpeakerDetector`
- `IGameWindowCapture`
- `IOcrService`

For v0.19, prioritize tests for:

- existing CLI tests continuing to pass;
- parsing `--gui` without changing default console behavior;
- pure runtime status, layout-state, view-model, or navigation helper logic if extracted;
- state transitions where practical: `Idle`, `Starting`, `Detecting`, `Reduced`, `Restored`, `Stopping`, and `Error`;
- guarded real audio start eligibility and confirmation behavior continuing to pass;
- existing simulated detection audio GUI behavior continuing to work;
- existing guarded real audio GUI behavior continuing to work;
- existing Paddle OCR backend selection/cache behavior continuing to work;
- CLI/core real audio safety behavior remaining unchanged;
- stop/close restore orchestration using fake audio services only;
- no automated test instantiating real `WindowsAudioMuteService` for system audio control;
- no test requiring manual UI clicks;
- no test controlling real audio, requiring real Tesseract, requiring real Paddle OCR execution, or requiring a real game window.

Manual verification for v0.19 must be explicit and local. Default GUI launch must not run real audio. Guarded real audio manual verification may only be done intentionally with explicit UI confirmation and must preserve restore behavior. Visual verification should include the persistent dock being visible across pages, guarded real-audio controls being accessible without navigating to the Audio page, logs readability, and status updates for OCR/detection/audio state.

Existing v0.5 speaker matching tests should continue covering:

- matching `流浪者`;
- matching `流浪者：`;
- matching whitespace/newline wrapped `流浪者`;
- matching `Wanderer` case-insensitively;
- non-match for `旅行者`;
- non-match for empty text;
- non-match for an empty target list;
- CLI parsing for `--detect-speaker-once` and `--speaker-text`.

Existing v0.1 tests should continue covering:

- target speaker starts speaking;
- target speaker stops speaking;
- non-target speaker speaks;
- unknown speaker does not newly mute/filter;
- repeated speaker frames do not cause repeated audio API calls;
- restore is safe when called multiple times;
- mute/reduce is safe when called multiple times;
- exception during detection does not break recovery logic;
- cancellation or shutdown attempts to restore audio.

For later OCR phases, test at least:

- OCR jitter does not cause rapid mute/unmute;
- OCR failure does not leave audio permanently muted;
- OCR raw text normalization is separated from speaker matching.

Do not write automated tests that modify real system audio.

Do not weaken or delete existing tests to make the build pass.

If tests are not added for new core logic, explain why.

## MVP rules

For early prototypes:

- Prefer simulated speaker input before adding OCR.
- Prefer console or minimal UI before full desktop UI.
- Build the smallest working vertical slice:
  1. detect or simulate speaker name;
  2. decide whether to apply audio filtering;
  3. mute or reduce target process/audio session;
  4. restore audio;
  5. log state changes.

The v0.1 audio MVP, v0.2 local JSON configuration, v0.3 window capture prototype, v0.4 OCR text extraction prototype, v0.5 speaker detection prototype, v0.6 OCR-driven detection dry-run, v0.7 detection stability gate, v0.8 simulated audio integration, v0.9 guarded real audio integration, v0.10 manual OCR region calibration, v0.11 OCR region source resolution, v0.12 configuration integration, v0.13 usability hardening, v0.14 minimal WinForms control panel, v0.15 GUI hardening, v0.16 GUI guarded real audio page, v0.17 WPF modern GUI shell, v0.18 OCR backend replacement, v0.18.1 OCR backend diagnostic stabilization, v0.19 WPF persistent control dock, and v0.19.1 CaptureLost UI recovery are implemented; v0.19.2 should preserve them while improving foreground UX and Resume/Reconnect.

Do not add masking, persistent settings UI, GUI settings editor, config editing/saving, global hotkeys, tray icon, always-on-top mini status window, new OCR backend architecture, model inference, speaker recognition from image, automatic region detection, fabricated preset coordinates, default real audio behavior, unguarded `WindowsAudioMuteService` integration, or gameplay automation during v0.19.2. Limited foreground switching input via `SendInput` / `Alt+Tab` is allowed only when explicitly requested, user-visible/configurable, and not used for gameplay commands.

Do not optimize prematurely.

Do not build a framework before the next behavior is verified.

## Logging rules

Log important state transitions:

- detected speaker changed;
- target speaker detected;
- non-target speaker detected;
- audio filter requested;
- mute requested;
- reduce-volume requested;
- restore requested;
- restore succeeded;
- restore skipped because already restored;
- repeated audio filter skipped because already active;
- detection error;
- audio control error;
- configuration load error;
- configuration validation error;
- CLI override applied where helpful;
- target window/process lookup;
- capture region selected;
- debug screenshot saved;
- capture error;
- OCR command requested;
- OCR input image selected or saved;
- OCR region selected;
- OCR region source selected;
- OCR region config loaded;
- OCR region preset selected;
- OCR region preset unavailable;
- OCR region source ambiguity;
- OCR raw text extracted;
- OCR error;
- speaker detection command requested;
- speaker text selected;
- speaker text normalized;
- speaker match result;
- dry-run command requested;
- dry-run iteration started;
- dry-run iteration completed;
- dry-run state changed;
- dry-run timing selected;
- raw match result observed;
- stability gate threshold selected;
- stable detection state changed;
- OCR region calibration command requested;
- calibration screenshot captured;
- calibration region selected;
- calibration region validation error;
- calibration JSON saved;
- calibration cancelled;
- cancellation requested;
- shutdown restore attempted.

Do not log sensitive user information.

Logs should help debug state transitions, configuration values, and timing issues.

Do not spam logs in tight loops.

## Git and change management rules

Keep changes small and reviewable.

Before large changes, explain the intended file list.

Prefer one task per commit.

Do not mix unrelated changes in one task.

Before finishing, inspect the diff and verify that no unrelated files were changed.

If using Codex worktrees or parallel tasks:

- avoid multiple agents editing the same files at the same time;
- avoid parallel changes to `.csproj`, `Program.cs`, `AppSettings`, and core interfaces unless coordinated;
- merge only after build/test verification.

## Documentation rules

Maintain these files when relevant:

- `README.md`: user-facing setup, run, and manual verification.
- `docs/ROADMAP.md`: project phases and milestone status.
- `docs/DECISIONS.md`: important technical decisions and reasons.
- `AGENTS.md`: agent instructions and engineering rules.
- `config.example.json`: example local configuration for safe startup and manual testing.

For meaningful behavior changes, update `README.md` or relevant docs.

For important architectural choices, update `docs/DECISIONS.md`.

For v0.19 implementation tasks, `README.md` should later document the persistent control dock, `docs/DECISIONS.md` should later record why common guarded real-audio controls moved out of the Audio page, and `docs/ROADMAP.md` should later update v0.19. Do not update those files during an AGENTS-only milestone update task.

Do not add excessive documentation for trivial changes.

## Done criteria

Before finishing a task:

- The project should build with `dotnet build`.
- If tests exist, run `dotnet test`.
- New core logic should have focused tests where practical.
- If a command cannot be run, state the exact reason.
- Verify that the task did not exceed the requested roadmap phase.
- Verify that no unrelated files were modified.
- Verify that default run remains safe and does not control real system audio unless explicitly enabled.
- Verify that the final response contains concrete command results, not assumptions.

The final response must include:

- changed files;
- what changed;
- commands run;
- result of each command;
- assumptions;
- untested behavior or known limitations.

## Final response format

After making code changes, respond in this format:

```text
Changed files:
- path/to/file.cs: what changed

Verification:
- dotnet restore: passed/failed/not run, with reason
- dotnet build: passed/failed/not run, with reason
- dotnet test: passed/failed/not run, with reason
- manual run: passed/failed/not run, with reason

Notes:
- assumptions
- limitations
- follow-up suggestions if needed
```

Do not say the task is complete if build or test verification failed, unless the failure is explicitly explained and the remaining issue is documented.
