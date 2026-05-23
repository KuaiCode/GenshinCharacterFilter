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

The current milestone is **v0.20.2 BetterGI-like Game Activation Flow**.

The previous **v0.1 Audio MVP**, **v0.2 Local JSON Configuration**, **v0.3 Window Capture Prototype**, **v0.4 OCR Text Extraction Prototype**, **v0.5 Speaker Detection from OCR Text Prototype**, **v0.6 OCR-driven Detection Dry Run**, **v0.7 Detection Stability Gate**, **v0.8 Simulated Audio Integration**, **v0.9 Guarded Real Audio Integration**, **v0.9.1 Partial Audio Apply Restore Fix**, **v0.10 Manual OCR Region Calibration**, **v0.11 OCR Region Source Resolution**, **v0.12 Configuration Integration**, **v0.13 Usability Hardening**, **v0.14 Minimal WinForms Control Panel**, **v0.15 GUI Hardening**, **v0.16 GUI Guarded Real Audio Page**, **v0.17 WPF Modern GUI Shell**, **v0.18 OCR Backend Replacement / Low-latency OCR Spike**, **v0.18.1 OCR Backend Diagnostic Stabilization**, **v0.19 WPF Persistent Control Dock / Interaction Layout**, **v0.19.1 CaptureLost UI Recovery**, **v0.19.2 Foreground UX / Resume Flow**, **v0.20 Capture Backend Spike / BetterGI-style Capture Backend Evaluation**, and **v0.20.1 WGC-first GUI Run Mode Bugfix** are considered implemented or stage-complete where applicable:

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
- v0.19.2 implemented foreground activation, optional explicit input-based foreground fallback, calibration selector foreground improvements, and basic Resume/Reconnect without changing real audio safety gates;
- v0.20 added capture backend abstraction and a Windows.Graphics.Capture spike, and that work may remain available as one capture option;
- v0.20.1 made WPF prefer WindowsGraphicsCapture for GUI testing, but user testing still showed WGC unavailable paths could make the main flow unusable;
- user testing still shows the primary usability gap is the lack of a reliable BetterGI-like "switch to game, then start" flow before calibration or detection;
- real audio safety gates remain unchanged;
- default target speakers are `流浪者` and `Wanderer`.

Scope for v0.20.2:

- Console app remains supported.
- GUI remains explicit via `--gui`.
- Keep existing CLI behavior.
- Keep the existing WPF shell, left navigation, and persistent control/status dock.
- Keep Paddle OCR backend behavior.
- Keep TesseractCli fallback.
- Keep guarded real audio safety gates unchanged.
- Preserve the v0.20 capture backend abstraction and Windows.Graphics.Capture work, but do not let WGC unavailability make the main GUI flow unusable.
- Shift the active focus to a BetterGI-like game activation/start flow.
- Add a first-class `SwitchToGame` service.
- Start, Calibrate, and Resume must call `SwitchToGame` before live capture.
- `SwitchToGame` must:
  - locate the YuanShen main window;
  - restore it if minimized;
  - try Win32 foreground activation;
  - try `AttachThreadInput`, `SetForegroundWindow`, and `BringWindowToTop` where appropriate;
  - use explicit `SendInput` / `Alt+Tab` fallback if enabled;
  - verify the foreground HWND belongs to YuanShen / `TargetProcessName`;
  - return structured success/failure.
- Fix `SendInput error: 87` by correcting interop definitions and the input sequence.
- If `SwitchToGame` fails, do not silently enter manual foreground fallback as the primary UX.
- If `SwitchToGame` fails, show a clear structured error and keep WPF usable.
- Detection must not start, and real audio must not be touched, unless target window activation and capture preflight succeed.
- Stop, CaptureLost, and Error paths must restore audio if audio may have been applied.
- Paddle OCR should auto warm up in WPF; manual warmup remains available only as retry/diagnostic.
- Do not default back to `VisiblePixels` as the main solution.
- Windows.Graphics.Capture, DXGI, GDI, and other capture backend work remains allowed, but capture backend work is not the only solution for this milestone.
- Do not control real audio by default.
- The persistent control dock should continue to show:
  - run state;
  - target process;
  - target speakers;
  - OCR engine;
  - OCR backend warm/status;
  - selected capture backend and capture backend status where practical;
  - last OCR text;
  - last detected speaker;
  - last audio action;
  - current audio state.
- Start Guarded Real Audio must still require:
  - an explicit enable checkbox or equivalent armed state;
  - confirmation dialog;
  - preflight;
  - valid OCR region;
  - stable detection only.
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
- Existing v0.19.2 foreground UX / Resume Flow behavior must remain stable where it still applies.
- Existing v0.20 capture backend behavior should remain available where practical, but it is not the sole main path for v0.20.2.
- Existing v0.20.1 WGC-first GUI behavior may be adjusted so the BetterGI-like `SwitchToGame` flow is the primary GUI start path.
- .NET 8.
- Windows x64.
- VS Code / Codex / Visual Studio friendly workflow.

Out of scope for the current milestone:

- GUI config editor.
- Saving edited config.
- Full GUI config editor / saving edited config.
- Global hotkeys.
- Tray icon.
- Always-on-top mini status window.
- Changing OCR backend architecture.
- Removing Tesseract CLI fallback.
- New feature work outside the BetterGI-like game activation/start flow.
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
- No gameplay automation.
- Auto-clicking, auto-dialogue skipping, combat automation, task automation, macro loops, or game-control input automation remain out of scope.
- Sending gameplay commands remains out of scope.
- Automating dialogue, combat, daily tasks, quests, movement, or other gameplay decisions remains out of scope.
- `SendInput` / `Alt+Tab` for foreground switching is allowed when explicitly scoped, user-visible/configurable, and limited to bringing the target window to the foreground.
- `SendInput` / `Alt+Tab` foreground switching is not gameplay automation when it does not send gameplay commands.
- Automatic gameplay decision-making.
- DXGI / BitBlt implementation unless explicitly scoped as a separate later backend.
- DirectX hooks.
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
- Logs remain readable, large enough, and copyable.
- `SwitchToGame` exists as a first-class service.
- Calibrate OCR Region calls `SwitchToGame` before live capture.
- Dry-run, simulated detection audio, guarded real audio, and Resume/Reconnect call `SwitchToGame` before live detection.
- `SwitchToGame` can locate, restore, activate, and verify the target YuanShen window, returning structured success/failure.
- Win32 activation, `AttachThreadInput`, `SetForegroundWindow`, `BringWindowToTop`, and explicit `SendInput` / `Alt+Tab` fallback are handled as foreground switching only.
- `SendInput error: 87` is addressed by corrected interop and input sequencing.
- If `SwitchToGame` fails, the GUI remains usable, detection does not start, and real audio is not touched.
- WGC backend work remains available where practical, but WGC unavailability does not make the main GUI flow unusable.
- Paddle OCR auto warm-up is attempted in WPF before detection; manual warm-up remains a diagnostic/retry action.
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
- No arbitrary heavy dependencies are introduced.
- No hook/injection/game memory/gameplay automation is introduced.
- Explicitly verify no hook/injection/game memory/gameplay automation is introduced.
- No third-party UI dependencies, WinUI, OpenCV, ONNX, masking, overlay, game-control automation, game memory access, hooking, injection, or DirectX hooks are introduced.
- The final response reports changed files, verification commands, assumptions, and limitations.

Do not implement later roadmap phases until this milestone works.

## Tech stack

- Language: C#
- Runtime: .NET 8 or newer
- Target OS: Windows
- Target architecture: Windows x64
- Initial app type: console app
- GUI option for current milestone: WPF shell with capture backend selection launched explicitly with `--gui`
- WinForms may remain for the existing calibration selector or temporary fallback only.
- Later GUI options: WinUI only if explicitly requested in a future milestone
- Audio control: NAudio or Windows Core Audio APIs
- Configuration: local JSON using .NET built-in JSON support unless a stronger reason is documented
- Capture backend spike may use Windows.Graphics.Capture and required Windows desktop interop.
- Windows.Graphics.Capture backend must be isolated behind interfaces.
- If WGC requires WinRT, COM, or Direct3D resources, isolate them in the concrete capture backend implementation.
- Tests must use fake capture backends and must not require real WGC availability.
- Image processing later: TBD
- OCR later: TBD
- Packaging later: TBD

Do not assume administrator privileges unless explicitly required and explained.

Do not introduce a GUI config editor, config saving, global hotkeys, tray icon, always-on-top mini window, overlay, gameplay automation, new OCR backend architecture, image-processing dependency, or model-inference dependency during the v0.20.2 BetterGI-like Game Activation Flow milestone. WPF remains a thin shell over existing services. Windows.Graphics.Capture / DirectX capture backend work is allowed as a non-invasive capture backend spike; DirectX hooks remain prohibited unless explicitly discussed and approved. `SendInput` / `Alt+Tab` foreground switching is allowed only when explicitly scoped to `SwitchToGame`, user-visible/configurable, and limited to bringing the target window to the foreground. Avoid OpenCV, ONNX, WinUI, and third-party UI dependencies for this milestone.

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
23. v0.20 Capture Backend Spike / Windows.Graphics.Capture evaluation.
24. v0.20.1 WGC-first GUI run mode bugfix.
25. v0.20.2 BetterGI-like Game Activation Flow.
26. Future global hotkey / tray / status mini-window.
27. Future optional DXGI / BitBlt backend evaluation if WGC is insufficient.
28. Stable mute/unmute coordination with debounce and recovery.
29. Optional masking.

Do not implement later-phase functionality prematurely.

For every task, identify which phase is being worked on and avoid touching unrelated phases.

## Core modules

Use these module boundaries unless the user asks for a different design:

- `IGameWindowCapture`: captures frames from the target game window.
- `CaptureBackend` or equivalent enum: selects `VisiblePixels`, `WindowsGraphicsCapture`, and future explicitly scoped backends such as `BitBlt` or `DxgiDesktopDuplication`.
- `CaptureBackendOptions`: stores selected backend, fallback policy, and backend-specific settings without leaking them into detection/audio logic.
- `IGameCaptureBackend` or equivalent: encapsulates concrete live-capture backend implementation.
- `IGameWindowCaptureSessionFactory`: upper-level factory used by calibration and detection to create capture sessions from configured backend options.
- `WindowsGraphicsCaptureBackend` or equivalent: isolated WGC spike implementation.
- `SwitchToGame` service or equivalent: first-class game activation/start-flow service used before live calibration, detection, guarded real audio, and Resume/Reconnect.
- `SwitchToGameResult` / structured activation result: reports success, target not found, still minimized, foreground mismatch, activation denied, `SendInput` failure, timeout, or unknown error.
- `TargetWindowActivator` / Win32 activation helper: locates the target process window, restores it, attempts foreground activation, and verifies the foreground owner without leaking UI concerns into detection/audio logic.
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

For v0.20.2, expected work is limited to:

- preserving the v0.19 WPF persistent control/status dock and left navigation behavior;
- preserving the v0.19.1 CaptureLost UI recovery behavior;
- preserving v0.19.2 foreground UX behavior where still applicable;
- preserving v0.20 capture backend abstraction and Windows.Graphics.Capture behavior where practical;
- adding a first-class BetterGI-like `SwitchToGame` flow before live calibration/detection starts;
- making calibration, dry-run, simulated audio, guarded real audio, and Resume/Reconnect call `SwitchToGame` before live capture;
- fixing explicit `SendInput` / `Alt+Tab` foreground fallback for window switching only;
- preventing detection startup and real audio creation when `SwitchToGame` or capture preflight fails;
- keeping `VisiblePixels` available but not treating it as the main solution for v0.20.2;
- keeping fixed-image OCR mode independent from live capture backend;
- ensuring `DetectionDryRunLoop` depends only on capture abstractions such as `IGameWindowCaptureSession`, not concrete backend types;
- using existing core services and `GuiCommandService` or an equivalent shared application service;
- preserving CLI behavior and existing command-line tests;
- preserving config and CLI merge behavior;
- preserving Paddle OCR backend behavior and Tesseract CLI fallback;
- preserving guarded real audio safety gates;
- preserving existing simulated audio GUI mode;
- preserving existing guarded real audio GUI mode;
- adding tests for `SwitchToGame`, activation fallback, foreground verification, safe failure, and status helpers where practical.

Do not create a GUI config editor, save edited config, add global hotkeys, add a tray icon, add an always-on-top mini status window, enable real audio by default, change OCR backend architecture, remove Tesseract CLI fallback, detect regions automatically, fabricate preset coordinates, implement DirectX hooks, send gameplay commands, automate gameplay, or weaken existing real-audio guard flags during v0.20.2.

## OCR region source rules

For v0.20.2 BetterGI-like Game Activation Flow, preserve these rules:

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

For v0.20.2 BetterGI-like Game Activation Flow, preserve these rules:

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
- Capture backend logic must be replaceable.
- Concrete capture backend implementation must not leak into detection or audio logic.
- `DetectionDryRunLoop` must not know whether frames come from `VisiblePixels` or `WindowsGraphicsCapture`.
- Capture backend failures must be structured and logged.
- WPF must remain responsive when capture backend initialization or frame acquisition fails.
- WGC-specific resources must be disposed.
- Capture backend selection must not bypass OCR region validation.
- Capture backend selection must not bypass guarded real audio safety gates.
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

For v0.20:

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
- Existing v0.19 persistent control dock behavior must remain stable.
- Existing v0.19.1 CaptureLost UI recovery behavior must remain stable.
- Existing v0.19.2 foreground UX behavior must remain stable where applicable.
- Do not add fuzzy matching yet.

## Safety rules

- Never modify game files.
- Do not inject code into the game process.
- Do not read or modify game memory.
- Do not implement anti-cheat bypass logic.
- Do not implement hook-based gameplay automation.
- Do not implement DirectX hooks unless explicitly discussed and approved later.
- DirectX hooks remain prohibited unless explicitly discussed and approved.
- Windows.Graphics.Capture / DirectX capture backend is allowed as a non-invasive capture backend spike.
- Windows.Graphics.Capture / DirectX capture backend and DirectX hooks are not the same thing.
- Do not implement behavior that automates gameplay decisions.
- Do not add auto-clicking, auto-dialogue skipping, combat automation, task automation, macro loops, or game-control input automation.
- Foreground switching is allowed when explicitly scoped to `SwitchToGame`.
- `SendInput` / `Alt+Tab` foreground switching is allowed only when user-visible/configurable and only for bringing the target window to the foreground.
- `SendInput` / `Alt+Tab` foreground switching must never send gameplay commands and must not become gameplay automation.
- Prefer screen capture, OCR, and OS-level audio session control over invasive game modification.
- If a requested feature requires invasive game modification, explain the risk and propose non-invasive alternatives instead.
- Do not store sensitive user data, credentials, cookies, tokens, or game login information.
- Do not send game data or screenshots to external services unless explicitly requested and reviewed.
- Screen capture is allowed only for local processing unless the user explicitly requests external services.
- Do not send screenshots to cloud services.
- Do not add telemetry.

## Configuration rules

Store user configuration in a local JSON file unless the project already uses another configuration format.

For v0.20.2:

- Local JSON configuration is implemented and should now cover common OCR, detection loop, stability threshold, audio filter, and OCR region source defaults.
- The WPF shell may select and validate config files, display effective runtime status, run `SwitchToGame`, select capture backend for current runs, and provide a persistent control dock, but it must not become a persistent settings editor.
- The WPF shell milestone must not add full config editing or saving behavior.
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
- Default capture backend should remain `VisiblePixels` unless the user explicitly changes it.
- Invalid capture backend names should produce clear validation errors.
- If `WindowsGraphicsCapture` is selected and unavailable, fallback is allowed only when `AllowBackendFallback = true`; otherwise show a clear error.
- Effective config / current run settings should display selected capture backend.

Configuration should include at least:

- `TargetProcessName`
- `TargetSpeakers`
- `RealAudioEnabled`
- `AudioFilter.Mode`
- `AudioFilter.VolumePercent`
- `Ocr`
- `Detection`
- `Capture` or `Detection.CaptureBackend`, depending on the implementation shape

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

Suggested `Capture` fields:

- `Backend`: `"VisiblePixels"` or `"WindowsGraphicsCapture"`
- `AllowBackendFallback`: `true` or `false`

v0.4/v0.5/v0.6/v0.7/v0.8/v0.9/v0.10/v0.11/v0.12/v0.13/v0.14/v0.15/v0.16/v0.17/v0.18/v0.18.1/v0.19/v0.19.1/v0.19.2/v0.20 OCR, speaker debug, dry-run, stability-gate, simulated audio, guarded real audio, calibration, region source, configuration integration, usability hardening, minimal control panel, GUI hardening, GUI guarded real audio, WPF modern shell, OCR backend replacement, OCR backend diagnostic stabilization, persistent control dock, CaptureLost UI recovery, Foreground UX, and capture backend behavior may include:

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
- capture backend selection and explicit backend fallback policy.
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
- unknown capture backend;
- invalid capture backend fallback policy.

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

For v0.20.2:

- Use built-in .NET JSON support where practical.
- Existing NAudio dependency for real Windows audio control may remain.
- Do not add arbitrary heavy dependencies.
- If Windows.Graphics.Capture requires built-in Windows SDK / WinRT interop, document why.
- Do not add third-party capture libraries without explicit justification.
- BetterGI code may be referenced, copied, or adapted only if its license is reviewed and complied with.
- BetterGI is GPL-3.0 licensed; copying/adapting GPL code can impose GPL obligations on the distributed combined/derived work.
- Prefer architecture-level reference and independent implementation when possible to avoid unnecessary license coupling.
- If BetterGI code is copied or adapted:
  - preserve copyright notices;
  - preserve GPL-3.0 license text or required notices;
  - document copied/adapted files and origins;
  - update README/DECISIONS/NOTICE or equivalent attribution;
  - ensure distribution obligations are understood before release.
- Do not mix BetterGI GPL code into this project silently.
- Do not remove or obscure BetterGI attribution.
- Any copied third-party code must be tracked with license and attribution.
- Before copying GPL code, decide whether the project is willing to distribute under GPL-compatible terms.
- If the project should avoid GPL coupling, use BetterGI only as architectural reference and implement independently.
- Existing minimal WinForms-based calibration window may remain.
- WPF may be used for the GUI shell because the project already targets Windows desktop APIs.
- Existing WinForms code may remain as calibration selector or fallback while WPF shell evolves.
- Use built-in WPF styling, templates, resources, and Windows APIs where practical instead of external UI dependencies.
- Do not add external GUI dependencies.
- Win32 foreground activation and `SendInput` / `Alt+Tab` fallback should use in-project interop or existing platform APIs; do not add a dependency only to send foreground-switching input unless explicitly justified.
- Preserve the existing OCR provider abstraction and Paddle/Tesseract selection behavior.
- Do not change OCR backend architecture or add heavy OCR/model dependencies without explicit approval.
- If using Tesseract CLI, do not vendor traineddata files into the repository.
- If using Windows OCR APIs, document package identity or deployment limitations.
- Do not send screenshots to cloud OCR services.
- Do not add OpenCV.
- Do not add ONNX Runtime.
- Do not add third-party WPF control libraries.
- Do not add WinUI dependencies.
- Global hotkey work is deferred and is not prohibited long-term.
- Do not add a global hotkey dependency during v0.20.
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

For v0.20.2, prioritize tests for:

- existing CLI tests continuing to pass;
- parsing `--gui` without changing default console behavior;
- `SwitchToGame` success path;
- minimized target restore path;
- Win32 activation failure -> explicit `SendInput` / `Alt+Tab` fallback path;
- `SendInput` failure producing a structured error;
- foreground verification rejecting the wrong foreground window;
- Calibrate OCR Region calling `SwitchToGame` before live capture;
- guarded real audio calling `SwitchToGame` before detection;
- detection not starting if `SwitchToGame` fails;
- real audio not being created or touched if `SwitchToGame` fails;
- Stop, Error, and CaptureLost restoring audio if audio may have been applied;
- WPF remaining responsive after activation failure;
- Paddle OCR auto warm-up path in WPF, with manual warmup remaining diagnostic;
- fixed-image mode not using live capture backend;
- capture backend failure not hanging the loop;
- capture backend failure attempting safe restore through fake audio if audio may have been applied;
- GUI-selected capture backend flowing into command options;
- WPF status model displaying capture backend/status where practical;
- guarded real audio start eligibility and confirmation behavior continuing to pass;
- existing simulated detection audio GUI behavior continuing to work;
- existing guarded real audio GUI behavior continuing to work;
- existing Paddle OCR backend selection/cache behavior continuing to work;
- CLI/core real audio safety behavior remaining unchanged;
- stop/close restore orchestration using fake audio services only;
- no automated test instantiating real `WindowsAudioMuteService` for system audio control;
- no test requiring manual UI clicks;
- no automated test requiring a real YuanShen window;
- no automated test requiring real WGC availability;
- no test controlling real audio, requiring real Tesseract, requiring real Paddle OCR execution, or requiring a real game window.

Manual verification for v0.20.2 must be explicit and local. Default GUI launch must not run real audio. Guarded real audio manual verification may only be done intentionally with explicit UI confirmation and must preserve restore behavior. Visual verification should include `SwitchToGame` attempts, Win32 activation logs, optional `SendInput` / `Alt+Tab` fallback logs, foreground HWND/process verification, calibration startup, guarded real audio startup preflight, persistent dock readability, and status updates for OCR/detection/audio/capture state.

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

The v0.1 audio MVP, v0.2 local JSON configuration, v0.3 window capture prototype, v0.4 OCR text extraction prototype, v0.5 speaker detection prototype, v0.6 OCR-driven detection dry-run, v0.7 detection stability gate, v0.8 simulated audio integration, v0.9 guarded real audio integration, v0.10 manual OCR region calibration, v0.11 OCR region source resolution, v0.12 configuration integration, v0.13 usability hardening, v0.14 minimal WinForms control panel, v0.15 GUI hardening, v0.16 GUI guarded real audio page, v0.17 WPF modern shell, v0.18 OCR backend replacement, v0.18.1 OCR backend diagnostic stabilization, v0.19 WPF persistent control dock, v0.19.1 CaptureLost UI recovery, v0.19.2 Foreground UX / Resume Flow, v0.20 Capture Backend Spike, and v0.20.1 WGC-first GUI Run Mode are implemented or stage-complete; v0.20.2 should preserve them while adding a BetterGI-like `SwitchToGame` activation flow.

Do not add masking, persistent settings UI, GUI settings editor, config editing/saving, global hotkeys, tray icon, always-on-top mini status window, new OCR backend architecture, model inference, speaker recognition from image, automatic region detection, fabricated preset coordinates, default real audio behavior, unguarded `WindowsAudioMuteService` integration, DirectX hooks, gameplay commands, or gameplay automation during v0.20.2. Windows.Graphics.Capture / DirectX capture backend is not prohibited as a non-invasive capture backend spike, but hook/injection/game memory/gameplay automation remain prohibited. `SendInput` / `Alt+Tab` foreground switching is allowed only for `SwitchToGame` and must never send gameplay commands.

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

For v0.20.2 implementation tasks, `README.md` should later document the `SwitchToGame` flow, Win32 activation, explicit `SendInput` / `Alt+Tab` foreground fallback, failure diagnostics, and unchanged real audio safety gates. `docs/DECISIONS.md` should later record why WGC alone was insufficient for the main GUI UX and why a BetterGI-like activation/start flow was introduced. `docs/ROADMAP.md` should later update v0.20.2 and keep future DXGI / BitBlt evaluation if WGC is insufficient. Do not update those files during an AGENTS-only milestone update task.

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
