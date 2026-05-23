# GenshinCharacterFilter

GenshinCharacterFilter is a Windows console accessibility/preferences utility prototype for muting or reducing target process audio when a configured character is speaking.

Current milestone: **v0.20 Capture Backend Spike / BetterGI-style Capture Backend Evaluation**.

The v0.1 Audio MVP through v0.19.2 Foreground UX / Resume Flow milestones are implemented or stage-complete where applicable. v0.20 introduces a BetterGI-style multi-backend capture abstraction and a Windows.Graphics.Capture spike while real-audio safety gates remain unchanged.

## Current Scope

- Console app with an explicit modern GUI shell.
- Simulated speaker input.
- Target process audio filtering through `IAudioMuteService`.
- Audio modes: `Mute` and `ReduceVolume`.
- Local JSON configuration.
- CLI arguments can override JSON configuration.
- Explicit one-shot debug screenshot capture.
- Explicit one-shot OCR raw text extraction from a local image.
- Explicit one-shot speaker detection from manual text or OCR raw text.
- Explicit OCR-driven detection dry-run loop for observing OCR and matching stability.
- Stability-gated dry-run output using consecutive match/miss thresholds.
- Explicit simulated detection audio mode using stable state only.
- Guarded real detection audio mode using stable state only.
- Explicit manual OCR region calibration that saves pixel and ratio coordinates.
- Unified OCR region source resolution through absolute pixels, calibration JSON, or preset selector.
- Local JSON configuration for OCR, detection loop, stability thresholds, OCR region source, and audio filter defaults.
- Explicit configuration validation and effective configuration diagnostics.
- Preflight checks for common OCR, image, region config, and process problems.
- Explicit modern WPF GUI shell launched with `--gui`, with Overview, Config, OCR, Detection, Audio, and Logs pages.
- Optional `PaddleOcrLocal` backend for low-latency OCR experiments, with `TesseractCli` retained as the default fallback.
- OCR benchmark and failure-sample diagnostics for comparing backend speed and inspecting bad crops.
- Persistent WPF control/status dock visible across pages for guarded real audio controls, run state, OCR backend status, last OCR text, last detected speaker, and last audio action.
- Best-effort Win32 foreground activation before calibration and live detection startup.
- Optional, explicit input foreground fallback for window switching only; it is disabled by default.
- CaptureLost Resume/Reconnect from the WPF dock.
- Capture backend selection through config and WPF:
  - `VisiblePixels`, the existing foreground visible-pixel path.
  - `WindowsGraphicsCapture`, an isolated spike with clear diagnostics and optional configured fallback.

Out of scope: GUI config editor, saving edited config, default automatic real audio, config-only guarded real audio, unguarded real detection audio, production auto mute, automatic OCR region detection, fabricated preset coordinates, fuzzy matching, speaker recognition from image, WinUI, overlay, masking, hotkeys, tray icon, always-on-top mini window, DXGI / BitBlt backend implementation, DirectX hooks, game memory access, hooks, DLL injection, game file modification, and gameplay automation. Windows.Graphics.Capture is treated as a non-invasive capture backend spike, not as a hook or game integration.

## Commands

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj
```

## Safe Default Run

Running without arguments uses built-in safe defaults:

- `RealAudioEnabled = false`
- `TargetProcessName = GenshinImpact`
- `TargetSpeakers = 流浪者, Wanderer`
- `AudioFilter.Mode = Mute`
- `AudioFilter.VolumePercent = 30`

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj
```

Default mode is simulated and does not change real system audio.

## Modern WPF GUI Shell

The modern local GUI only runs when `--gui` is supplied. Default console startup is unchanged.

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --gui
```

The WPF shell uses a BetterGI-inspired local tool layout with left navigation, a top status bar, card-style pages, light/dark theme detection at startup, a dedicated Logs page, and a visually separated Guarded Real Audio Danger Zone. It provides controls for selecting a config file, validating config, showing effective config, calibrating the OCR region, testing OCR once, starting/stopping dry-run detection, starting/stopping simulated detection audio, and starting guarded real audio after explicit confirmation. It shows command logs in the window and uses the same underlying config, OCR, calibration, detection, and audio services as the CLI.

The persistent control dock stays visible while switching pages. It shows run state, current audio state, OCR engine, OCR backend warm status, requested/actual capture backend, capture status, last OCR text, last detected speaker, and last audio action. Start Guarded Real Audio, Stop, Restore, and Resume/Reconnect are available from this dock so they are not hidden inside the Audio page during live play. Runtime configuration controls stay on their pages; the capture backend selector and backend fallback option are on the Detection page. Start Guarded Real Audio still requires arming the guarded checkbox, passing preflight, a valid OCR region, and an explicit confirmation dialog; raw OCR matches never directly control real audio.

The GUI is a modern shell, not a settings editor or config editor. It displays run status, config, target process, speakers, capture mode, and real-audio status; prevents overlapping detection starts; logs button errors in the panel; auto-scrolls new log output; and lets Stop request cancellation for cancellable operations. The Guarded Real Audio controls are visually separated as a danger zone and still require explicit enablement plus confirmation.

The OCR page can select the OCR backend for the current GUI run. Choose `TesseractCli` for the compatibility fallback, or `PaddleOcrLocal` for the low-latency local backend. The `Warm up OCR backend` button initializes the selected backend before detection starts; for Paddle this avoids putting the slow first model load into the first detection frame. The GUI logs the actual backend used by dry-run, simulated audio, and guarded real audio detection.

For OCR input, select the original screenshot, such as `debug-captures/capture-latest.png`. Do not use `debug-ocr/ocr-input-latest.png` as OCR input because that file is the generated debug output and may be overwritten by the next OCR run.

The OCR input image field is used by Test OCR Once. Detection loops use live target-process capture by default, so each iteration captures the current target window before OCR. The `Use fixed image for detection loop` checkbox is only for dry-run or simulated fixed-image debugging; leave it unchecked for real-time game/window detection.

In the GUI, the detection tuning fields override `config.local.json` for the current run only and are not saved. The default GUI tuning is meant for live dialogue testing: Run until Stop is checked, Loop interval ms is `200`, Capture delay ms is `100`, Match threshold is `2`, Miss threshold is `1`, and Save debug images is unchecked. When Run until Stop is checked, GUI detection ignores config `LoopCount` for that run. When it is unchecked, enter a positive Loop count for a short fixed-count test; other tuning fields still override config only for that run.

Loop interval is the delay after one iteration completes, not the full detection cycle time. The actual cycle also includes live screenshot capture, OCR, speaker matching, and any audio action. Detection logs include per-iteration timing for capture, OCR, match, audio, and total elapsed time so you can identify the slow step before changing thresholds.

Save debug images is useful for troubleshooting OCR regions, but it writes `debug-captures/capture-latest.png` and `debug-ocr/ocr-input-latest.png` during loops and can make realtime detection slower. Leave it unchecked for realtime GUI dry-run, simulated detection audio, and guarded real audio; the loop will use temporary OCR input files only as needed by Tesseract.

Live detection prefers region-only capture when an OCR region source is configured. In that mode the loop resolves the calibration ratio or absolute OCR region against the current target window size, captures only that small region, and passes that small temporary image to Tesseract. Logs show `Capture mode: region-only`; if region-only capture cannot be used, logs show `Capture mode: full-window fallback` with the reason. If OCR timing still dominates after region-only capture, the remaining bottleneck is likely Tesseract CLI startup/recognition time.

The Guarded Real Audio section is disabled by default. To start it, enable the guarded real audio checkbox, pass config/preflight checks, provide a valid OCR region source, and confirm the warning dialog. The GUI uses stable detection results only; raw OCR/speaker matches never directly control real audio. Stop or closing the window requests cancellation and attempts restore through the existing cleanup path, but Windows audio sessions can still require manual mixer recovery if restore fails.

Use reduce-volume mode for the first real audio test. The GUI does not create `WindowsAudioMuteService` until the guarded real audio start action is explicitly confirmed. Guarded real audio does not allow fixed-image detection mode, because a static screenshot must not drive real system audio.

Guarded real audio includes OCR jitter tolerance for near target-speaker text. Strong OCR matches can enter the stable matched state, while weak near-matches and short/empty noisy OCR are used to avoid rapid restore/reduce flicker after a stable target hit. Clear non-target text can still restore quickly according to the configured miss threshold.

Capture, calibration, and live detection use visible screen pixels when the `VisiblePixels` backend is selected. The target window must be restored, visible, and not covered. For Genshin, prefer windowed or borderless window mode; exclusive fullscreen can minimize or block visible-pixel capture when focus moves to the GUI. Before calibration or live detection startup, the WPF GUI first minimizes itself and tries Win32 foreground activation for `TargetProcessName`. The `Enable Alt+Tab / SendInput foreground fallback` checkbox is off by default; when enabled, the app may use a limited Alt+Tab/SendInput foreground-switching attempt after Win32 activation fails. This fallback is only for bringing the target window to the foreground and does not send gameplay commands. The log clearly reports whether input fallback is disabled, attempted, succeeded, or failed. If automatic activation still fails, the GUI falls back to the manual foreground flow: it prompts you, minimizes the WPF window, waits briefly while you manually switch to the target, then captures or initializes detection from the current foreground window only if its process matches `TargetProcessName`. This applies to calibration, dry-run detection, simulated detection audio, and guarded real audio startup; guarded real audio still requires the checkbox, preflight, and confirmation first. For detection fallback, the WPF window stays minimized until detection stops or fails so it does not immediately steal focus back from the game. If you switch back to WPF during detection, the game may minimize again and visible-pixel capture can stop. When this capture-lost state is detected, the app stops detection safely, attempts audio restore if needed, keeps the WPF UI usable, shows CaptureLost plus Restored/not-filtering state in the dock, and enables Resume/Reconnect from the persistent dock when the last run context can be reused. Resume/Reconnect creates a fresh foreground capture session and does not reuse the old window handle; guarded real audio resume still requires the guarded enablement and confirmation flow. Windowed/borderless mode, a second monitor, or future hotkey/status-window work can reduce the need to switch back to WPF during detection.

v0.20 begins that separate capture backend spike by adding selection, diagnostics, fallback policy, and real Windows.Graphics.Capture frame acquisition around `VisiblePixels` and `WindowsGraphicsCapture`.

## Capture Backend Selection

v0.20 adds a capture backend boundary inspired by BetterGI-style multi-backend capture tools. BetterGI publicly documents an `IGameCapture`-style capture abstraction with BitBlt, WindowsGraphicsCapture, and DXGI Desktop Duplication implementations, plus window-management helpers and configurable capture-mode selection. For this milestone, the implementation was written independently against Microsoft Windows.Graphics.Capture APIs instead of copying BetterGI code, to avoid unnecessary GPL-3.0 license coupling. No BetterGI dependencies are added and no BetterGI gameplay automation features are implemented.

Available backend names:

- `VisiblePixels`: the existing Win32/BitBlt visible-pixel capture path. It remains the default and preserves current behavior. It still requires the target window to be visible enough for screen-pixel capture.
- `WindowsGraphicsCapture`: a real Windows.Graphics.Capture HWND backend spike. It creates a `GraphicsCaptureItem` for the target window, captures a frame through a Direct3D11-backed frame pool, writes a PNG compatible with the existing OCR pipeline, and supports region capture for realtime loops. If WGC cannot initialize or acquire a frame, the app reports a structured backend error instead of hanging the WPF UI.

Config example:

```json
"Capture": {
  "Backend": "VisiblePixels",
  "AllowBackendFallback": false,
  "CaptureTimeoutMs": 2000
}
```

CLI overrides:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --detect-loop --capture-backend WindowsGraphicsCapture --allow-capture-backend-fallback
```

Fallback is explicit. If `WindowsGraphicsCapture` is selected and unavailable, or if WGC frame acquisition fails before a live run starts, the app falls back to `VisiblePixels` only when `AllowBackendFallback` is `true` or `--allow-capture-backend-fallback` is supplied. Logs show requested backend, actual backend, and fallback reason. Otherwise it reports a clear capture backend error. Fixed-image OCR/debug mode does not use a live capture backend.

The WPF Detection page selects the capture backend for the current run and chooses whether backend fallback is allowed. The persistent dock only displays requested/actual backend and capture status so runtime controls are not duplicated. `Print Current Run Settings` prints the config backend, GUI selected backend, current requested backend, fallback setting, last actual backend, and whether a GUI capture backend override is active. Calibration and detection startup first log `GUI selected capture backend: ...`, then backend creation logs `Requested capture backend: ...` and `Actual capture backend: ...`.

When `WindowsGraphicsCapture` is selected, WPF calibration and detection startup bypass Win32 foreground activation and manual foreground fallback. The WGC path looks up the target HWND, creates a `GraphicsCaptureItem`, and tries to acquire a WGC frame. Successful detection logs should show `Capture mode: wgc-window` or `wgc-window-region-only`. If you see `foreground-region-only`, the run used `VisiblePixels`, usually because `WindowsGraphicsCapture` failed and explicit fallback was enabled; the log should include the fallback reason. If fallback is disabled, WGC failures are reported directly and do not fall back to manual foreground capture. Minimized windows can still fail depending on Windows/game behavior; in that case the error reports `TargetMinimized` or another WGC-specific reason instead of the old visible-pixel capture-lost message.

Windows.Graphics.Capture is not a DirectX hook. The project still does not inject into the game, read or write game memory, modify game files, send screenshots to cloud services, or automate gameplay. DirectX hooks remain out of scope unless explicitly discussed and approved later. Future work may separately evaluate DXGI / BitBlt backends if WGC is insufficient.

After OCR region calibration succeeds, the app also writes `debug-ocr/calibration-region-latest.png`. Open that preview to confirm the saved region actually contains the speaker-name text before testing OCR or real audio.

## Debug Screenshot Capture

Screenshot mode only runs when `--capture-once` is supplied. It does not control real system audio.

Verify with a normal desktop app such as Notepad before trying a game window:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --capture-once --process notepad --capture-output debug-captures --capture-delay-ms 500
```

The app looks for the target process main window, attempts to restore and activate it, waits briefly, then saves `capture-latest.png` under the output directory. The target window should be restored, visible, and not covered by other windows. If Windows blocks foreground activation, manually bring the target window to the front before capturing. If the target is minimized and cannot be restored, the error explains that this is visible-pixel capture and suggests manually restoring the target or using windowed/borderless mode. The expected v0.3 output is a full-window debug screenshot including the title bar and visible frame. This is visible-window screen capture, not background window capture, so covered windows may still capture the covering pixels.

## OCR Text Extraction

OCR mode only runs when `--ocr-once` is supplied. It reads an existing local image, prints raw OCR text, and does not control real system audio. v0.4 does not connect OCR output to speaker detection, `MuteCoordinator`, mute, or restore behavior.

The first OCR provider is the external Tesseract CLI. Install Tesseract separately and make `tesseract` available on `PATH`, or pass `--tesseract-path <path>`. This repository does not vendor tessdata files. For Simplified Chinese OCR, install the `chi_sim` language data; English OCR uses `eng`. The default OCR language is `chi_sim+eng`, and the default page segmentation mode is `7`, which is intended for a single line or small amount of text.

Whole-image OCR can pick up window menus, status bars, and other UI text. Prefer `--ocr-region <x,y,width,height>` or `--ocr-region-config <path>` to crop the OCR input to the text area you want to inspect. Absolute regions use image coordinates with `0,0` at the top-left. Calibration files use the saved ratio region to compute pixels for the current input image size. When a region is resolved, the cropped debug image is saved as `debug-ocr/ocr-input-latest.png`; inspect that file to confirm what is actually sent to OCR.

For the current v0.4 OCR path, prefer a raw cropped image first. Manual testing found that keeping the original cropped image can preserve anti-aliased Chinese strokes better than extra preprocessing. For Chinese-only lines, try `--ocr-lang chi_sim` before `chi_sim+eng`; mixed language mode can make Tesseract prefer English-looking guesses. The final cropped image sent to OCR is written to `debug-ocr/ocr-input-latest.png` whenever `--ocr-region` is enabled.

Manual OCR verification with an existing screenshot:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-input debug-captures/capture-latest.png --ocr-lang chi_sim+eng
```

Manual OCR verification with a cropped region:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-input debug-captures/capture-latest.png --ocr-region 50,80,700,120 --ocr-lang chi_sim+eng --ocr-psm 7 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

Recommended Chinese OCR debugging with a raw cropped region:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

Manual OCR verification using a calibration file:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-input debug-captures/capture-latest.png --ocr-region-config ocr-region.json --ocr-lang chi_sim --ocr-psm 7 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

If Tesseract is not installed or the requested language data is missing, the app reports a clear OCR error. No screenshots are sent to cloud OCR services.

## OCR Backend Selection

The OCR service is selected by `Ocr.Engine` in config or by `--ocr-engine`.

- `TesseractCli` is the default and remains the compatibility fallback. It starts `tesseract.exe` for each OCR call, so it is stable but can be slow in realtime loops.
- `PaddleOcrLocal` uses the local PaddleOCRSharp runtime in-process. It is intended as a low-latency candidate because the OCR engine can be initialized once and reused by a detection loop.

Config example:

```json
"Ocr": {
  "Engine": "PaddleOcrLocal",
  "TesseractExecutablePath": "tesseract",
  "PaddleModelDirectory": null,
  "PaddleRuntimeDirectory": null,
  "Language": "chi_sim",
  "PageSegmentationMode": 7,
  "RegionConfigPath": "ocr-region.json"
}
```

`PaddleModelDirectory` and `PaddleRuntimeDirectory` are optional. Leave them empty to use the bundled PaddleOCRSharp model/runtime files copied by NuGet. If you provide custom paths, the app validates them and reports clear errors for missing native DLLs, model files, or unsupported architecture. You can also test from CLI:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-engine PaddleOcrLocal --ocr-input debug-captures/capture-latest.png --ocr-region-config ocr-region.json
```

Realtime detection timing still prints `OCR: <ms>` per iteration. Compare the same OCR region with `TesseractCli` and `PaddleOcrLocal` before enabling guarded real audio.

Detection loops print the selected OCR engine before the first iteration:

- `OCR engine: TesseractCli` or `OCR engine: PaddleOcrLocal`
- `OCR backend initialized: true/false`
- `OCR backend warm: true/false`
- Paddle model/runtime paths when Paddle is selected

If `PaddleOcrLocal` is selected and initialization fails, the app reports the Paddle failure and does not silently fall back to Tesseract. Switch `Ocr.Engine` back to `TesseractCli` when you need the known fallback path.

In WPF, select `PaddleOcrLocal` on the OCR page and click `Warm up OCR backend` before starting realtime detection. Paddle first-run initialization can take many seconds, while warm runs are the timing that matters for realtime detection. Start/Stop detection keeps the warmed Paddle service alive for the GUI app lifetime unless you switch engines or change relevant OCR runtime/model settings.

The WPF backend status is keyed by OCR engine plus Paddle model/runtime settings. If you change `PaddleModelDirectory` or `PaddleRuntimeDirectory`, the UI treats that as a different backend instance and shows it as not initialized until it is warmed up.

PaddleOCRSharp may print a third-party community-edition banner during runtime initialization. The app suppresses managed stdout/stderr output during the narrow Paddle engine creation step so this banner does not look like an application error. If a native component writes directly to the process console and bypasses .NET console redirection, the banner may still appear; it is diagnostic third-party output, not a GenshinCharacterFilter failure. Real Paddle initialization exceptions are still reported normally.

## OCR Benchmark And Failure Samples

Use `--ocr-benchmark` to compare engines against the same local crop. This does not start detection or audio.

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-benchmark --ocr-input debug-ocr/calibration-region-latest.png --ocr-engine TesseractCli --ocr-repeat 5
```

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-benchmark --ocr-input debug-ocr/calibration-region-latest.png --ocr-engine PaddleOcrLocal --ocr-repeat 5
```

The benchmark prints the engine, region source, preprocessing settings, raw text for each run, first-run elapsed time, average elapsed time, warm-run average elapsed time, min/max elapsed time, and failure count. For Paddle, the first run includes model/runtime initialization; warm-run average is the more useful realtime estimate. Use `debug-ocr/calibration-region-latest.png` or `debug-ocr/ocr-input-latest.png` to isolate OCR accuracy from capture timing.

Realtime detection can save failed OCR samples without saving every frame. Enable `Detection.SaveOcrFailureSamples` in config, or use the GUI checkbox when available. When OCR output is empty, noisy, or does not match a configured target speaker, the app writes:

- `debug-ocr/failures/YYYYMMDD-HHMMSS-iteration-xxxx.png`
- a matching `.json` sidecar with timestamp, OCR engine, raw text, normalized text, target speakers, OCR region, elapsed ms, and iteration

Failure samples are diagnostics only. If saving them fails, the detection loop logs a warning and continues.

OCR input preparation is optional and disabled by default because earlier manual samples sometimes worked better as raw crops. These config/CLI values can be used for one-shot OCR, benchmark, and detection loops:

- `Ocr.InputScale` / `--ocr-input-scale <1..4>`
- `Ocr.PaddingPixels` / `--ocr-padding-pixels <0..100>`
- `Ocr.Grayscale` / `--ocr-grayscale`
- `Ocr.Invert` / `--ocr-invert`
- `Ocr.Threshold` / `--ocr-threshold <0..255|none>`

Default behavior is still raw crop: scale `1`, padding `0`, grayscale `false`, invert `false`, threshold `none`.

## Speaker Detection Debug

Speaker detection mode only runs when `--detect-speaker-once` is supplied. v0.5 only normalizes text and matches it against configured target speakers; it does not mute, restore, or call `MuteCoordinator`.

Manual text verification without Tesseract:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --detect-speaker-once --speaker-text "流浪者："
```

OCR plus speaker detection debug path:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --detect-speaker-once --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

The output includes raw text, normalized text, whether a target speaker matched, and the matched speaker name. Matching is intentionally simple: trim whitespace, ignore common leading/trailing speaker punctuation such as `:` and `：`, match English names case-insensitively, and allow exact or contains matching. It does not use fuzzy matching.

Contains matching is debug-only in v0.5. A matched result does not automatically mute and must not be wired directly into automatic audio control. Before any auto-mute integration, the project needs stricter speaker-label parsing, OCR region confidence, debounce/hysteresis, or a gated match mode to avoid false positives from noisy OCR text.

## OCR-driven Detection Dry Run

Dry-run mode only runs when `--detect-loop` is supplied. It repeatedly runs OCR plus speaker matching, prints each raw match result, then applies a stability gate before reporting stable matched/not-matched state. It does not control real system audio, does not create a real audio service, and does not call `MuteCoordinator`.

Fixed image dry run:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --detect-loop --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

Window capture dry run:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --detect-loop --process notepad --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

Use `--loop-interval-ms <number>` to control timing. The allowed range is 100 to 10000 ms. Use `--loop-count <number>` to run a fixed number of iterations; omit it to run until Ctrl+C. Use `--match-threshold <number>` and `--miss-threshold <number>` to require consecutive raw matches or misses before the stable state changes; the allowed range is 1 to 10 and the default is 2 for both. Process capture mode should use `--ocr-region`, `--ocr-region-config`, or a configured preset so the loop observes the intended text area instead of full-window UI noise.

The output includes raw matched, raw matched speaker, stable matched, stable matched speaker, stable state changed, consecutive match count, and consecutive miss count. The stable signal is still observation-only unless `--simulate-audio-from-detection` is explicitly supplied.

## Simulated Detection Audio

Simulated detection audio mode only runs when `--simulate-audio-from-detection` is supplied together with `--detect-loop`. It uses the stability-gated detection state to request simulated mute/restore through `LoggingAudioMuteService`; it does not create `WindowsAudioMuteService` and does not control real system audio. If `--real-audio` is supplied with this mode, the command is rejected.

Fixed image simulated audio run:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --simulate-audio-from-detection --detect-loop --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

The output includes raw matched, raw matched speaker, stable matched, stable matched speaker, and `Simulated audio action: none|mute|restore`. Raw contains matching below the stability threshold does not request simulated audio. Repeated stable matched/not-matched states do not repeatedly spam simulated mute/restore. Real audio integration remains a later phase.

## Guarded Real Detection Audio

Guarded real detection audio is disabled by default. It runs only when the required CLI safety flags are supplied:

- `--detect-loop`
- `--real-audio`
- `--allow-real-audio-from-detection`

The target process can be provided either by CLI with `--process <target>` or by `TargetProcessName` in the loaded config file. Real audio still cannot be enabled by config alone; `--real-audio` and `--allow-real-audio-from-detection` must be passed on the command line.

The mode uses the same OCR, speaker matching, and stability gate as dry-run mode. Only stable matched state can request real mute/reduce, and only stable not-matched state can request restore. Raw contains matching never directly controls audio. Guarded real detection audio requires a valid OCR region source through `--ocr-region`, `--ocr-region-config`, or a configured preset. The app prints a clear warning before starting and attempts restore on shutdown or cancellation. If audio apply fails after it has started, shutdown/cancellation still attempts restore where possible.

Recommended first manual test target is a normal browser audio session such as Chrome, not the game:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --detect-loop --real-audio --allow-real-audio-from-detection --process chrome --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --audio-mode reduce --volume-percent 30 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

When using a local config with `TargetProcessName`, the process flag can be omitted:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --detect-loop --real-audio --allow-real-audio-from-detection --loop-count 5
```

Do not use guarded real audio until the OCR region and stable detection output have already been checked in dry-run or simulated mode. The implementation controls only target process audio sessions through `WindowsAudioMuteService`; it does not inject into a game, read memory, hook rendering, modify files, or simulate keyboard/mouse input.

## Manual OCR Region Calibration

Calibration mode only runs when `--calibrate-ocr-region` is supplied. It captures one target window screenshot, opens a minimal local selection window, lets you drag-select the speaker-name region, and saves both pixel and ratio coordinates to JSON. It does not run OCR, create `WindowsAudioMuteService`, call `MuteCoordinator`, or control real system audio. If `--real-audio` is supplied with calibration, the command is rejected.

Recommended first manual test target is Notepad:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --calibrate-ocr-region --process notepad --capture-output debug-captures --calibration-output ocr-region.json
```

Workflow:

- keep the target window visible and uncovered;
- drag a rectangle around the speaker-name text area;
- check the displayed `x, y, width, height` values;
- press Enter to save;
- press Esc to cancel.

The output JSON includes `sourceImageWidth`, `sourceImageHeight`, `regionPixels`, `regionRatio`, `generatedAt`, and `sourceProcessName`. Ratio coordinates are useful when the same relative speaker-name area must be reapplied after resolution or window-size changes.

Calibration also saves `debug-ocr/calibration-region-latest.png`, a crop from the selected rectangle. Use it to diagnose empty OCR output after calibration; if the preview does not contain the character name, recalibrate the region.

## OCR Region Source Resolution

OCR and detection modes can resolve their OCR region from one of these sources:

- `--ocr-region <x,y,width,height>`: absolute pixel region for the current image.
- `--ocr-region-config <path>`: local calibration JSON generated by `--calibrate-ocr-region`; the saved `regionRatio` is converted to pixels for the current image size.
- `--ocr-region-preset auto|2560x1600|1920x1080|none`: preset selector.

Priority is absolute pixels, then calibration JSON, then preset. Do not combine `--ocr-region` with `--ocr-region-config`. Do not combine `--ocr-region-config` with an explicit preset unless the preset is `none`.

Preset support is intentionally conservative. The only planned built-in preset names are `2560x1600` and `1920x1080`; preset coordinates must come from real calibration data and are not guessed. If a preset is selected but has no calibrated region, the app reports that the preset is not configured and asks you to run calibration or provide `--ocr-region-config`.

`--ocr-region-preset auto` matches only exact image sizes of `2560x1600` or `1920x1080`. Other resolutions should use manual calibration:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --calibrate-ocr-region --process notepad --capture-output debug-captures --calibration-output ocr-region.json
```

## Configuration File

Use the included safe example:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.example.json
```

`config.example.json` is safe by default: `RealAudioEnabled` is `false`, and it only provides OCR/detection defaults for explicit commands.

Validate a config without running OCR, detection, capture, or audio:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --validate-config
```

Print the merged effective config after CLI overrides without starting runtime work:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --print-effective-config
```

The effective config output includes `RealAudioEnabled`, `AllowRealAudioFromDetection`, and `Detection real audio allowed` so the real-audio safety gate is visible. Config alone cannot enable detection-driven real audio; `--real-audio` and `--allow-real-audio-from-detection` must still be passed on the command line.

Example shape:

```json
{
  "TargetProcessName": "GenshinImpact",
  "TargetSpeakers": ["流浪者", "Wanderer"],
  "RealAudioEnabled": false,
  "AudioFilter": {
    "Mode": "Mute",
    "VolumePercent": 30
  },
  "Ocr": {
    "Engine": "TesseractCli",
    "TesseractExecutablePath": "tesseract",
    "Language": "chi_sim",
    "PageSegmentationMode": 7,
    "InputScale": 1,
    "PaddingPixels": 0,
    "Grayscale": false,
    "Invert": false,
    "Threshold": null,
    "RegionPreset": "none"
  },
  "Detection": {
    "LoopIntervalMs": 500,
    "LoopCount": 5,
    "MatchThreshold": 2,
    "MissThreshold": 2,
    "SaveDebugImages": false,
    "SaveOcrFailureSamples": false,
    "EnableInputForegroundFallback": false
  },
  "Capture": {
    "Backend": "VisiblePixels",
    "AllowBackendFallback": false,
    "CaptureTimeoutMs": 2000
  }
}
```

For local use, copy `config.local.example.json` to `config.local.json` and adjust paths such as `TesseractExecutablePath` and `RegionConfigPath`. `config.local.json` is gitignored.

Short OCR once command using config defaults:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --ocr-once --ocr-input debug-captures/capture-latest.png
```

Short detection dry-run command using config defaults:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --detect-loop --loop-count 5
```

Guarded real audio still needs explicit CLI safety flags. Config cannot enable detection-driven real audio by itself:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.local.json --detect-loop --real-audio --allow-real-audio-from-detection --loop-count 5
```

Reduce-volume configuration remains available through `AudioFilter`:

```json
{
  "TargetProcessName": "GenshinImpact",
  "TargetSpeakers": ["流浪者", "Wanderer"],
  "RealAudioEnabled": false,
  "AudioFilter": {
    "Mode": "ReduceVolume",
    "VolumePercent": 30
  },
  "Ocr": {
    "Language": "chi_sim",
    "PageSegmentationMode": 7,
    "RegionConfigPath": "ocr-region.json"
  },
  "Detection": {
    "LoopIntervalMs": 500,
    "MatchThreshold": 2,
    "MissThreshold": 2,
    "SaveDebugImages": false,
    "EnableInputForegroundFallback": false
  }
}
```

The app does not create or overwrite user config files.

## CLI Overrides

CLI values override JSON values when supplied:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.example.json --process chrome --audio-mode reduce --volume-percent 25
```

Supported overrides:

- `--real-audio`
- `--gui`
- `--validate-config`
- `--print-effective-config`
- `--process <name>`
- `--audio-mode mute`
- `--audio-mode reduce`
- `--volume-percent <number>`
- `--capture-once`
- `--capture-output <directory>`
- `--capture-delay-ms <number>`
- `--capture-backend VisiblePixels|WindowsGraphicsCapture`
- `--allow-capture-backend-fallback`
- `--ocr-once`
- `--ocr-input <imagePath>`
- `--ocr-engine TesseractCli|PaddleOcrLocal`
- `--ocr-lang <language>`
- `--tesseract-path <path>`
- `--paddle-model-dir <path>`
- `--paddle-runtime-dir <path>`
- `--ocr-psm <number>`
- `--ocr-input-scale <number>`
- `--ocr-padding-pixels <number>`
- `--ocr-grayscale`
- `--ocr-invert`
- `--ocr-threshold <number|none>`
- `--ocr-benchmark`
- `--ocr-repeat <number>`
- `--ocr-region <x,y,width,height>`
- `--ocr-region-config <path>`
- `--ocr-region-preset auto|2560x1600|1920x1080|none`
- `--detect-speaker-once`
- `--speaker-text <text>`
- `--detect-loop`
- `--loop-interval-ms <number>`
- `--loop-count <number>`
- `--match-threshold <number>`
- `--miss-threshold <number>`
- `--simulate-audio-from-detection`
- `--allow-real-audio-from-detection`
- `--calibrate-ocr-region`
- `--calibration-output <path>`

`--real-audio` is the only way to enable runtime real audio from the CLI. `RealAudioEnabled` in config is kept as a documented setting, but config alone does not enable runtime real audio or guarded detection audio. The WinForms panel does not enable real audio by default and requires explicit guarded real audio confirmation.

## Real Audio Mode

Runtime real audio control is enabled only when `--real-audio` is passed:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --real-audio --process GenshinImpact
```

Real reduce-volume mode:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --real-audio --process GenshinImpact --audio-mode reduce --volume-percent 30
```

Real mode uses Windows Core Audio sessions through `WindowsAudioMuteService`. It controls only matching target process audio sessions. It does not inject into the game, read/write game memory, hook the process, modify game files, or simulate keyboard/mouse input.

## Manual Verification

- Enter `流浪者` or `Wanderer` to simulate a target speaker and request configured audio filtering.
- Enter the same target speaker again to confirm repeated target input does not repeat audio API calls.
- Enter another name, blank input, or `unknown` to request restore when filtered.
- Enter another non-target value to confirm repeated non-target input does not request restore again.
- Enter `q`, `quit`, or `exit` to leave; shutdown attempts restore.
- Run `--capture-once` against Notepad and confirm a full-window debug screenshot is written without enabling real audio. If activation is blocked, manually put Notepad in front and rerun the command.
- Run `--ocr-once` against an existing screenshot and confirm raw OCR text is printed without enabling real audio.
- Run `--ocr-once --ocr-region <x,y,width,height>` and confirm `debug-ocr/ocr-input-latest.png` contains only the intended OCR input region.
- For Chinese small text, first verify the raw cropped input in `debug-ocr/ocr-input-latest.png`; v0.4.1 does not apply scale, grayscale, or threshold preprocessing.
- Run `--detect-speaker-once --speaker-text "流浪者："` and confirm the debug output reports `Matched: True` without enabling real audio.
- Run a fixed-image `--detect-loop` with `--loop-count 5 --match-threshold 2 --miss-threshold 2` and confirm raw match output plus stable match output are printed without enabling real audio.
- Run `--simulate-audio-from-detection --detect-loop` with a fixed image and confirm simulated audio actions are printed without enabling real audio.
- Run `--calibrate-ocr-region --process notepad --calibration-output ocr-region.json` and confirm a local calibration JSON is saved without OCR or real audio.
- Run `--config config.local.json --validate-config` and confirm validation reports clear success or a specific config/preflight error.
- Run `--config config.local.json --print-effective-config` and confirm it prints merged settings without starting OCR, detection, or audio.
- Run `--gui` and confirm the control panel opens, can validate config, can print effective config, and can start/stop dry-run or simulated detection audio without enabling real audio.
- Only after separate confirmation, run guarded real detection audio against Chrome with `--real-audio --allow-real-audio-from-detection --process chrome` and confirm restore occurs on exit.

## Dependency Policy

NAudio is included only for Windows Core Audio session access in explicit real audio mode. It affects deployment by adding managed NuGet assemblies and Windows audio API access, but it does not add game integration, injection, memory reading, hooks, OCR, image-processing, model-inference, UI, or overlay features.

OCR keeps the external Tesseract CLI path as the default fallback. No Tesseract binaries or traineddata files are committed to the repository, and OCR output is not uploaded to any cloud service. v0.18 also includes PaddleOCRSharp as an optional local OCR backend spike; it adds native Windows x64 runtime/model artifacts through NuGet output copying, but remains opt-in through `Ocr.Engine` or `--ocr-engine PaddleOcrLocal`.
