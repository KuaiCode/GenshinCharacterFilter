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

The current milestone is **v0.10 Manual OCR Region Calibration**.

The previous **v0.1 Audio MVP**, **v0.2 Local JSON Configuration**, **v0.3 Window Capture Prototype**, **v0.4 OCR Text Extraction Prototype**, **v0.5 Speaker Detection from OCR Text Prototype**, **v0.6 OCR-driven Detection Dry Run**, **v0.7 Detection Stability Gate**, **v0.8 Simulated Audio Integration**, **v0.9 Guarded Real Audio Integration**, and **v0.9.1 Partial Audio Apply Restore Fix** are considered implemented and manually verified where applicable:

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
- stable detection can drive real audio only behind `--detect-loop`, `--real-audio`, `--allow-real-audio-from-detection`, and `--process <target>`;
- guarded real audio mute and reduce-volume modes were manually verified with Chrome;
- partial audio apply failure no longer skips shutdown/cancellation restore;
- default execution still does not screenshot or control real audio;
- OCR output is not connected to `MuteCoordinator` or automatic audio control;
- default target speakers are `流浪者` and `Wanderer`.

Scope for v0.10:

- Console app remains the main app.
- Add an explicit calibration mode only.
- Capture one screenshot from the target process/window.
- Display the screenshot in a minimal local calibration window.
- Let the user drag-select the OCR speaker-name region.
- Save both pixel region and ratio region to a local JSON file.
- Do not run OCR automatically unless explicitly requested.
- Do not control real audio in calibration mode.
- Do not call `MuteCoordinator` in calibration mode.
- Default run must remain safe.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- Existing v0.7 stability-gate behavior must remain stable.
- Existing v0.8 simulated audio behavior must remain stable.
- Existing v0.9 guarded real audio behavior must remain stable.
- .NET 8.
- Windows x64.
- VS Code / Codex / Visual Studio friendly workflow.

Out of scope for the current milestone:

- Full GUI application.
- WPF.
- WinUI.
- Overlay masking.
- Face detection.
- ONNX.
- OpenCV.
- Persistent settings UI or configuration UI.
- Automatic real audio during calibration.
- Gameplay automation.
- Keyboard/mouse automation.
- Input automation.
- Anti-cheat bypass.
- Game memory reading or modification.
- Hooking or injection.

Allowed for the current milestone:

- Minimal WinForms-based calibration window if needed.
- No external GUI dependency.
- No OpenCV.

Done when:

- `dotnet build` passes.
- If tests exist, `dotnet test` passes.
- Calibration mode is explicit and does not run during default startup.
- Calibration mode captures one screenshot from the configured target window/process.
- Calibration mode displays a minimal local selection window.
- The selected OCR region is saved as both pixel coordinates and ratio coordinates.
- The calibration output JSON is local and contains no sensitive data.
- Calibration mode does not run OCR unless explicitly requested.
- Calibration mode does not create `WindowsAudioMuteService`.
- Calibration mode does not control real audio.
- Calibration mode does not call `MuteCoordinator`.
- Default run remains safe and does not control real system audio.
- No WPF, WinUI, OpenCV, ONNX, masking, overlay, gameplay automation, game memory access, hooking, or injection is introduced.
- The final response reports changed files, verification commands, assumptions, and limitations.

Do not implement later roadmap phases until this milestone works.

## Tech stack

- Language: C#
- Runtime: .NET 8 or newer
- Target OS: Windows
- Target architecture: Windows x64
- Initial app type: console app
- Later GUI options: WPF or WinUI, but only after configuration and detection flows are stable and explicitly requested
- Audio control: NAudio or Windows Core Audio APIs
- Configuration: local JSON using .NET built-in JSON support unless a stronger reason is documented
- Image processing later: TBD
- OCR later: TBD
- Packaging later: TBD

Do not assume administrator privileges unless explicitly required and explained.

Do not introduce a full GUI application, overlay, gameplay automation, OCR model, image-processing dependency, or model-inference dependency during the v0.10 Manual OCR Region Calibration milestone. A minimal WinForms-based calibration window is allowed if needed; avoid OpenCV and ONNX for this milestone.

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
  - WPF / WinUI work later;
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
13. Stable mute/unmute coordination with debounce and recovery.
14. Minimal UI.
15. Optional masking.

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

For v0.10, expected work is limited to:

- explicit OCR region calibration mode, for example `--calibrate-ocr-region`
- capture of one target process/window screenshot
- minimal local calibration window for drag-selecting a rectangle
- saving source screenshot size, selected pixel region, and selected ratio region
- validation that x/y are >= 0, width/height are > 0, and the region is inside image bounds
- Esc cancels calibration
- Enter saves calibration after a valid region is selected
- tests for pixel-to-ratio conversion, ratio-to-pixel conversion, bounds validation, and calibration JSON
- no OCR by default in calibration mode
- no real audio control in calibration mode

Do not create `WindowsAudioMuteService`, call `MuteCoordinator`, run OCR, or control real audio during calibration mode unless a future task explicitly changes that scope.

## Speaker matching rules

For v0.9 guarded real audio integration:

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

For v0.10:

- Calibration mode must not create `WindowsAudioMuteService`.
- Calibration mode must not control real audio.
- Calibration mode must not call `MuteCoordinator`.
- Calibration mode must not run automatic mute/restore logic.
- Existing guarded real audio behavior must remain opt-in only.
- Existing simulated detection audio mode must remain available.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- Existing v0.7 stability-gate behavior must remain stable.
- Existing v0.8 simulated audio behavior must remain stable.
- Existing v0.9 guarded real audio behavior must remain stable.
- Do not add fuzzy matching yet.

## Safety rules

- Never modify game files.
- Do not inject code into the game process.
- Do not read or modify game memory.
- Do not implement anti-cheat bypass logic.
- Do not implement hook-based gameplay automation.
- Do not implement DirectX hooks unless explicitly discussed and approved later.
- Do not implement behavior that automates gameplay decisions.
- Do not add auto-clicking, auto-dialogue skipping, or input simulation unless explicitly requested later.
- Prefer screen capture, OCR, and OS-level audio session control over invasive game modification.
- If a requested feature requires invasive game modification, explain the risk and propose non-invasive alternatives instead.
- Do not store sensitive user data, credentials, cookies, tokens, or game login information.
- Do not send game data or screenshots to external services unless explicitly requested and reviewed.
- Do not add telemetry.

## Configuration rules

Store user configuration in a local JSON file unless the project already uses another configuration format.

For v0.10:

- Local JSON configuration is implemented and may be extended only when needed for OCR region calibration.
- Do not add a persistent settings UI.
- Do not store sensitive information.
- Do not include credentials, cookies, tokens, or game login data.
- Do not make network requests for configuration.
- Do not silently create or overwrite user configuration without explicit request.
- A sample configuration file such as `config.example.json` is allowed.
- If a default runtime config is created later, it must be safe by default.

Configuration should include at least:

- `TargetProcessName`
- `TargetSpeakers`
- `RealAudioEnabled`
- `AudioFilter.Mode`
- `AudioFilter.VolumePercent`

v0.4/v0.5/v0.6/v0.7/v0.8/v0.9/v0.10 OCR, speaker debug, dry-run, stability-gate, simulated audio, guarded real audio, and calibration configuration may include:

- OCR input image path or explicit capture input;
- OCR region;
- OCR debug output directory;
- OCR provider selection, only if a provider is actually introduced;
- speaker matcher options, only if they are needed for explicit debug behavior;
- dry-run timing options, only if needed for explicit dry-run behavior;
- match/miss stability thresholds, only if needed for explicit stability-gate dry-run behavior.
- simulated detection audio mode options, only if needed for explicit v0.8 behavior.
- guarded real audio detection options, only if needed for explicit v0.9 behavior.
- OCR region calibration output path, source screenshot size, pixel region, and ratio region, only if needed for explicit v0.10 calibration behavior.

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

For v0.10:

- Use built-in .NET JSON support where practical.
- Existing NAudio dependency for real Windows audio control may remain.
- Do not add new dependencies for v0.10 unless strongly justified.
- A minimal WinForms-based calibration window is allowed if needed.
- Do not add external GUI dependencies.
- Prefer an OCR provider abstraction so the engine can be replaced later.
- Do not add heavy OCR or model dependencies without explaining why.
- If using Tesseract CLI, do not vendor traineddata files into the repository.
- If using Windows OCR APIs, document package identity or deployment limitations.
- Do not send screenshots to cloud OCR services.
- Do not add OpenCV.
- Do not add ONNX Runtime.
- Do not add WPF or WinUI dependencies.
- Do not add global hotkey libraries.
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

For v0.10, prioritize tests for:

- pixel-to-ratio region conversion;
- ratio-to-pixel region conversion;
- region bounds validation;
- calibration JSON serialization and deserialization;
- explicit calibration command parsing, if CLI parsing is changed;
- calibration mode not enabling real audio, if orchestration is testable without UI;
- existing configuration, capture, OCR, speaker matching, and audio behavior remains unchanged.

Do not write automated tests that require a real game window.
Do not write automated tests requiring UI interaction.
Do not write automated tests that require a locally installed OCR engine unless the test can skip clearly.
Do not write tests that control real system audio.
No automated test may control real system audio.
Manual verification for v0.10 must be explicit, local, and must not run `--real-audio` automatically.

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

The v0.1 audio MVP, v0.2 local JSON configuration, v0.3 window capture prototype, v0.4 OCR text extraction prototype, v0.5 speaker detection prototype, v0.6 OCR-driven detection dry-run, v0.7 detection stability gate, v0.8 simulated audio integration, and v0.9 guarded real audio integration are implemented; v0.10 should preserve them while adding an explicit manual OCR region calibration tool.

Do not add masking, a full GUI application, persistent settings UI, model inference, speaker recognition from image, default real audio behavior, unguarded `WindowsAudioMuteService` integration, or gameplay automation during v0.10.

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

For v0.10 implementation tasks, `README.md` should document the calibration command, and `docs/DECISIONS.md` should record why calibration uses a minimal local window and saves a relative OCR region.

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
