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

The current milestone is **v0.1 Audio MVP**.

Scope:

- Console app only.
- Simulated speaker input only.
- Target process audio mute/restore only.
- .NET 8.
- Windows x64.
- VS Code / Codex / Visual Studio friendly workflow.
- Core mute coordination logic must be testable without changing real system volume.
- Real Windows audio control must be isolated behind `IAudioMuteService`.

Out of scope for the current milestone:

- OCR.
- Screen capture.
- WPF.
- WinUI.
- Overlay masking.
- Face detection.
- ONNX.
- OpenCV.
- Persistent settings UI.
- Gameplay automation.
- Keyboard/mouse automation.
- Anti-cheat bypass.
- Game memory reading or modification.

Done when:

- `dotnet build` passes.
- If tests exist, `dotnet test` passes.
- The app can simulate target speaker and non-target speaker input.
- The app can decide whether the current speaker should trigger mute.
- The app can request mute/restore through `IAudioMuteService`.
- The mute coordination logic is covered by focused tests where practical.
- Any real Windows audio implementation is isolated behind `IAudioMuteService`.
- The final response reports changed files, verification commands, assumptions, and limitations.

Do not implement later roadmap phases until this milestone works.

## Tech stack

- Language: C#
- Runtime: .NET 8 or newer
- Target OS: Windows
- Target architecture: Windows x64
- Initial app type: console app
- Later GUI options: WPF or WinUI, but only after the mute MVP is working and explicitly requested
- Audio control: NAudio or Windows Core Audio APIs
- Image processing later: TBD
- OCR later: TBD
- Packaging later: TBD

Do not assume administrator privileges unless explicitly required and explained.

Do not introduce OCR, image-processing, UI, model-inference, or automation dependencies during the v0.1 Audio MVP.

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
2. Window capture prototype.
3. OCR text extraction from a configurable screen region.
4. Speaker detection from OCR text.
5. Stable mute/unmute coordination with debounce and recovery.
6. Minimal UI and local configuration.
7. Optional screen-region masking prototype.
8. Optional dynamic mask or model-based detection prototype.

Do not implement later-phase functionality prematurely.

For every task, identify which phase is being worked on and avoid touching unrelated phases.

## Core modules

Use these module boundaries unless the user asks for a different design:

- `IGameWindowCapture`: captures frames from the target game window.
- `IOcrService`: extracts text from a specific screen region.
- `ISpeakerDetector`: determines the current speaker from OCR text or simulated input.
- `IAudioMuteService`: mutes and restores the target game process or audio session.
- `MuteCoordinator`: coordinates speaker detection, mute state, debounce logic, and recovery.
- `AppSettings`: stores target characters, target process name, OCR region, timing thresholds, and feature toggles.

For v0.1, only these are expected:

- `ISpeakerDetector`
- `IAudioMuteService`
- `MuteCoordinator`
- minimal settings or safe temporary console defaults

Do not create `IGameWindowCapture` or `IOcrService` implementations until the project reaches the corresponding phases.

## Architecture rules

- UI code must not directly call Windows audio APIs.
- Windows API, COM interop, OCR provider code, screen capture code, and overlay code must be isolated behind service interfaces.
- OCR logic must be replaceable.
- Do not hard-code one OCR provider into core business logic.
- Audio mute/restore logic must be reversible.
- The application must attempt to restore audio on cancellation, shutdown, and unexpected exceptions.
- Prefer explicit state machines for mute transitions instead of scattered boolean flags.
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

- Target speaker starts speaking → mute target process/audio session.
- Target speaker stops speaking → restore audio.
- Non-target speaker speaks → do not mute.
- Unknown speaker or detection failure → do not newly mute.
- Detection failures must not leave audio permanently muted.
- Repeated frames or repeated simulated inputs with the same speaker must not trigger repeated mute calls.
- OCR jitter must later be handled with debounce or hysteresis.
- Restore should be idempotent: calling restore multiple times should be safe.
- Mute should be idempotent: repeated mute requests while already muted should not spam the audio API.
- Shutdown, cancellation, and unexpected exceptions should attempt safe restore.

For v0.1:

- Use simulated speaker input.
- Implement mute/restore coordination using fake services first where practical.
- Add real audio control only behind `IAudioMuteService`.
- Do not add OCR-specific debounce logic until OCR exists.

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

For v0.1:

- Hard-coded safe defaults are acceptable only for temporary console testing.
- Do not hard-code target character names or process names inside reusable services.
- If configuration is added, use local JSON.
- Keep configuration minimal.

Long-term configuration should include at least:

- target process name;
- target character list;
- mute delay/debounce settings;
- restore delay/debounce settings;
- OCR region;
- feature toggles;
- mute mode;
- restore behavior;
- logging options.

Validate configuration values before using them.

Provide safe defaults for early prototypes.

Invalid configuration should produce a clear error message.

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

Do not add OCR, audio, image-processing, model-inference, capture, or UI dependencies without explaining:

- why the dependency is needed;
- why existing code is insufficient;
- whether the dependency affects deployment;
- whether it requires native runtime components;
- whether it affects Windows x64 packaging;
- whether there are licensing concerns.

Do not replace the project framework or UI stack without explicit approval.

For v0.1:

- An audio dependency such as NAudio may be considered only for real Windows audio control.
- Do not add OpenCV.
- Do not add OCR libraries.
- Do not add ONNX Runtime.
- Do not add WPF or WinUI dependencies.
- Do not add global hotkey libraries.

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

For v0.1, prioritize fake implementations for:

- `IAudioMuteService`
- `ISpeakerDetector`

Test at least:

- target speaker starts speaking;
- target speaker stops speaking;
- non-target speaker speaks;
- unknown speaker does not newly mute;
- repeated speaker frames do not cause repeated mute calls;
- restore is safe when called multiple times;
- mute is safe when called multiple times;
- exception during detection does not break recovery logic;
- cancellation or shutdown attempts to restore audio.

For later OCR phases, test at least:

- OCR jitter does not cause rapid mute/unmute;
- OCR failure does not leave audio permanently muted;
- OCR raw text normalization is separated from speaker matching.

Do not weaken or delete existing tests to make the build pass.

If tests are not added for new core logic, explain why.

## MVP rules

For early prototypes:

- Prefer simulated speaker input before adding OCR.
- Prefer console or minimal UI before full desktop UI.
- Build the smallest working vertical slice:
  1. detect or simulate speaker name;
  2. decide whether to mute;
  3. mute target process/audio session;
  4. restore audio;
  5. log state changes.

Do not add masking, OCR, complex UI, persistent settings UI, or model inference before the mute MVP works.

Do not optimize prematurely.

Do not build a framework before the core behavior is verified.

## Logging rules

Log important state transitions:

- detected speaker changed;
- target speaker detected;
- non-target speaker detected;
- mute requested;
- mute succeeded;
- mute skipped because already muted;
- restore requested;
- restore succeeded;
- restore skipped because already restored;
- detection error;
- audio control error;
- cancellation requested;
- shutdown restore attempted.

Do not log sensitive user information.

Logs should help debug state transitions and timing issues.

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

For meaningful behavior changes, update `README.md` or relevant docs.

For important architectural choices, update `docs/DECISIONS.md`.

Do not add excessive documentation for trivial changes.

## Done criteria

Before finishing a task:

- The project should build with `dotnet build`.
- If tests exist, run `dotnet test`.
- New core logic should have focused tests where practical.
- If a command cannot be run, state the exact reason.
- Verify that the task did not exceed the requested roadmap phase.
- Verify that no unrelated files were modified.
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
