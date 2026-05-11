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

The current milestone is **v0.7 Detection Stability Gate**.

The previous **v0.1 Audio MVP**, **v0.2 Local JSON Configuration**, **v0.3 Window Capture Prototype**, **v0.4 OCR Text Extraction Prototype**, **v0.5 Speaker Detection from OCR Text Prototype**, and **v0.6 OCR-driven Detection Dry Run** are considered implemented and manually verified:

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
- default execution still does not screenshot or control real audio;
- OCR output is not connected to `MuteCoordinator` or automatic audio control;
- default target speakers are `流浪者` and `Wanderer`.

Scope for v0.7:

- Console app only.
- Add stability gate / debounce / hysteresis for OCR-driven speaker detection.
- Require consecutive matched frames before stable target-present state.
- Require consecutive non-matched frames before stable target-absent state.
- Print raw match result and stable match state.
- Keep dry-run behavior.
- Do not control real audio.
- Do not call `MuteCoordinator` yet.
- Do not automatically mute.
- Default run must remain safe.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- .NET 8.
- Windows x64.
- VS Code / Codex / Visual Studio friendly workflow.

Out of scope for the current milestone:

- Automatic mute/restore based on OCR.
- `MuteCoordinator` integration.
- Real audio mute/restore based on OCR.
- Background capture.
- Fuzzy matching unless explicitly requested.
- Speaker recognition from image.
- Production audio control.
- GUI.
- Overlay masking.
- Face detection.
- ONNX.
- OpenCV.
- Persistent settings UI or configuration UI.
- Gameplay automation.
- Keyboard/mouse automation.
- Anti-cheat bypass.
- Game memory reading or modification.
- Hooking or injection.

Done when:

- `dotnet build` passes.
- If tests exist, `dotnet test` passes.
- Speaker detection mode is explicit and does not run during default startup.
- Dry-run mode is explicit and does not run during default startup.
- The app can compute stable target-present / target-absent state from consecutive raw match results.
- The app prints raw match result and stable match state.
- Stability-gated dry-run verification does not control real system audio.
- Stable state changes are not connected to `MuteCoordinator` or automatic mute/restore behavior.
- No fuzzy matching, GUI, masking, overlay, gameplay automation, game memory access, hooking, or injection is introduced.
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

Do not introduce UI, overlay, gameplay automation, OCR model, image-processing, or model-inference dependencies during the v0.7 Detection Stability Gate milestone. Avoid OpenCV and ONNX for this milestone.

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
10. Stable mute/unmute coordination with debounce and recovery.
11. Minimal UI.
12. Optional masking.

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

For v0.7, expected work is limited to:

- `DetectionStabilityGate` or an equivalent small type
- `DetectionStabilityOptions` or an equivalent small options type
- `DetectionStableState`, `DetectionStabilityResult`, or equivalent result/state types
- consecutive matched-frame threshold before stable target-present state
- consecutive non-matched-frame threshold before stable target-absent state
- dry-run loop integration to report raw match result and stable state
- tests for consecutive match / non-match thresholds and state-change reporting

Do not connect stable detection results to `MuteCoordinator` or automatic audio filtering during v0.7.

## Speaker matching rules

For v0.7 stability-gated dry-run matching:

- Trim whitespace.
- Handle newlines around text.
- Ignore common trailing speaker punctuation such as `:` and `：`.
- English matching should be case-insensitive.
- Exact match and simple contains match are allowed.
- Contains matching is debug-only and must not directly drive automatic audio control.
- Stable state may be computed from raw match results, but still must not control audio.
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

For v0.7:

- Do not modify `MuteCoordinator`.
- Do not call `MuteCoordinator` from the dry-run loop.
- Do not connect raw or stable dry-run results to audio mute/restore behavior.
- Do not automatically mute or restore based on OCR output or speaker match output.
- Existing v0.1 audio, v0.2 configuration, and v0.3 capture behavior must remain stable.
- Existing v0.4 OCR behavior must remain stable.
- Existing v0.5 speaker matching behavior must remain stable.
- Existing v0.6 dry-run behavior must remain stable.
- Do not add automatic audio control or fuzzy matching yet.

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

For v0.7:

- Local JSON configuration is implemented and may be extended only when needed for stability-gate options.
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

v0.4/v0.5/v0.6/v0.7 OCR, speaker debug, dry-run, and stability-gate configuration may include:

- OCR input image path or explicit capture input;
- OCR region;
- OCR debug output directory;
- OCR provider selection, only if a provider is actually introduced;
- speaker matcher options, only if they are needed for explicit debug behavior;
- dry-run timing options, only if needed for explicit dry-run behavior;
- match/miss stability thresholds, only if needed for explicit stability-gate dry-run behavior.

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

For v0.7:

- Use built-in .NET JSON support where practical.
- Existing NAudio dependency for real Windows audio control may remain.
- Do not add new dependencies for v0.7 unless strongly justified.
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

For v0.7, prioritize tests for:

- consecutive matches transitioning from stable NotMatched to stable Matched;
- a single match not transitioning when match threshold is greater than 1;
- consecutive misses transitioning from stable Matched to stable NotMatched;
- same stable state not repeatedly emitting state-changed;
- stability threshold validation;
- dry-run loop orchestration with fake OCR/matcher where practical;
- command-line parsing for explicit stability threshold flags;
- existing configuration, OCR, speaker matching, and mute/reduce coordination behavior remains unchanged.

Do not write automated tests that require a real game window.
Do not write automated tests that require a locally installed OCR engine unless the test can skip clearly.
Manual verification for v0.7 should use explicit dry-run commands with a provided debug image or an explicit capture path, print raw match result and stable match state, and must not modify system audio.

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

The v0.1 audio MVP, v0.2 local JSON configuration, v0.3 window capture prototype, v0.4 OCR text extraction prototype, v0.5 speaker detection prototype, and v0.6 OCR-driven detection dry-run are implemented; v0.7 should preserve them while adding a stability gate before any audio integration.

Do not add masking, complex UI, persistent settings UI, model inference, speaker recognition from image, automatic mute/restore from OCR, `MuteCoordinator` integration, or gameplay automation during v0.7.

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

For v0.7 implementation tasks, `README.md` should document stability-gate dry-run commands and `docs/DECISIONS.md` should record that v0.7 adds detection stability before any audio integration.

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
