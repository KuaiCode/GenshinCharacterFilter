# GenshinCharacterFilter

GenshinCharacterFilter is a Windows console accessibility/preferences utility prototype for muting or reducing target process audio when a configured character is speaking.

Current milestone: **v0.9 Guarded Real Audio Integration**.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, v0.3 Window Capture Prototype, v0.4 OCR Text Extraction Prototype, v0.5 Speaker Detection from OCR Text Prototype, v0.6 OCR-driven Detection Dry Run, v0.7 Detection Stability Gate, and v0.8 Simulated Audio Integration are implemented. v0.9 allows stable detection to control real Windows audio only behind multiple explicit opt-in flags.

## Current Scope

- Console app only.
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

Out of scope: default automatic real audio, unguarded real detection audio, production auto mute, fuzzy matching, speaker recognition from image, WPF, WinUI, overlay, masking, hotkeys, game memory access, hooks, DLL injection, game file modification, and input automation.

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

## Debug Screenshot Capture

Screenshot mode only runs when `--capture-once` is supplied. It does not control real system audio.

Verify with a normal desktop app such as Notepad before trying a game window:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --capture-once --process notepad --capture-output debug-captures --capture-delay-ms 500
```

The app looks for the target process main window, attempts to restore and activate it, waits briefly, then saves `capture-latest.png` under the output directory. The target window should be restored, visible, and not covered by other windows. If Windows blocks foreground activation, manually bring the target window to the front before capturing. The expected v0.3 output is a full-window debug screenshot including the title bar and visible frame. This is visible-window screen capture, not background window capture, so covered windows may still capture the covering pixels.

## OCR Text Extraction

OCR mode only runs when `--ocr-once` is supplied. It reads an existing local image, prints raw OCR text, and does not control real system audio. v0.4 does not connect OCR output to speaker detection, `MuteCoordinator`, mute, or restore behavior.

The first OCR provider is the external Tesseract CLI. Install Tesseract separately and make `tesseract` available on `PATH`, or pass `--tesseract-path <path>`. This repository does not vendor tessdata files. For Simplified Chinese OCR, install the `chi_sim` language data; English OCR uses `eng`. The default OCR language is `chi_sim+eng`, and the default page segmentation mode is `7`, which is intended for a single line or small amount of text.

Whole-image OCR can pick up window menus, status bars, and other UI text. Prefer `--ocr-region <x,y,width,height>` to crop the OCR input to the text area you want to inspect. The region uses image coordinates with `0,0` at the top-left. When a region is supplied, the cropped debug image is saved as `debug-ocr/ocr-input-latest.png`; inspect that file to confirm what is actually sent to OCR.

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

If Tesseract is not installed or the requested language data is missing, the app reports a clear OCR error. No screenshots are sent to cloud OCR services.

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

Use `--loop-interval-ms <number>` to control timing. The allowed range is 100 to 10000 ms. Use `--loop-count <number>` to run a fixed number of iterations; omit it to run until Ctrl+C. Use `--match-threshold <number>` and `--miss-threshold <number>` to require consecutive raw matches or misses before the stable state changes; the allowed range is 1 to 10 and the default is 2 for both. Process capture mode requires `--ocr-region` so the loop observes the intended text area instead of full-window UI noise.

The output includes raw matched, raw matched speaker, stable matched, stable matched speaker, stable state changed, consecutive match count, and consecutive miss count. The stable signal is still observation-only unless `--simulate-audio-from-detection` is explicitly supplied.

## Simulated Detection Audio

Simulated detection audio mode only runs when `--simulate-audio-from-detection` is supplied together with `--detect-loop`. It uses the stability-gated detection state to request simulated mute/restore through `LoggingAudioMuteService`; it does not create `WindowsAudioMuteService` and does not control real system audio. If `--real-audio` is supplied with this mode, the command is rejected.

Fixed image simulated audio run:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --simulate-audio-from-detection --detect-loop --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

The output includes raw matched, raw matched speaker, stable matched, stable matched speaker, and `Simulated audio action: none|mute|restore`. Raw contains matching below the stability threshold does not request simulated audio. Repeated stable matched/not-matched states do not repeatedly spam simulated mute/restore. Real audio integration remains a later phase.

## Guarded Real Detection Audio

Guarded real detection audio is disabled by default. It runs only when all required flags are supplied:

- `--detect-loop`
- `--real-audio`
- `--allow-real-audio-from-detection`
- `--process <target>`

The mode uses the same OCR, speaker matching, and stability gate as dry-run mode. Only stable matched state can request real mute/reduce, and only stable not-matched state can request restore. Raw contains matching never directly controls audio. The app prints a clear warning before starting and attempts restore on shutdown or cancellation. If audio apply fails after it has started, shutdown/cancellation still attempts restore where possible.

Recommended first manual test target is a normal browser audio session such as Chrome, not the game:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --detect-loop --real-audio --allow-real-audio-from-detection --process chrome --ocr-input debug-captures/capture-latest.png --ocr-region 10,150,700,120 --ocr-lang chi_sim --ocr-psm 7 --loop-count 5 --loop-interval-ms 500 --match-threshold 2 --miss-threshold 2 --audio-mode reduce --volume-percent 30 --tesseract-path "C:\Program Files\Tesseract-OCR\tesseract.exe"
```

Do not use guarded real audio until the OCR region and stable detection output have already been checked in dry-run or simulated mode. The implementation controls only target process audio sessions through `WindowsAudioMuteService`; it does not inject into a game, read memory, hook rendering, modify files, or simulate keyboard/mouse input.

## Configuration File

Use the included safe example:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --config config.example.json
```

Example shape:

```json
{
  "TargetProcessName": "GenshinImpact",
  "TargetSpeakers": ["流浪者", "Wanderer"],
  "RealAudioEnabled": false,
  "AudioFilter": {
    "Mode": "Mute",
    "VolumePercent": 30
  }
}
```

Reduce-volume configuration:

```json
{
  "TargetProcessName": "GenshinImpact",
  "TargetSpeakers": ["流浪者", "Wanderer"],
  "RealAudioEnabled": false,
  "AudioFilter": {
    "Mode": "ReduceVolume",
    "VolumePercent": 30
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
- `--process <name>`
- `--audio-mode mute`
- `--audio-mode reduce`
- `--volume-percent <number>`
- `--capture-once`
- `--capture-output <directory>`
- `--capture-delay-ms <number>`
- `--ocr-once`
- `--ocr-input <imagePath>`
- `--ocr-lang <language>`
- `--tesseract-path <path>`
- `--ocr-psm <number>`
- `--ocr-region <x,y,width,height>`
- `--detect-speaker-once`
- `--speaker-text <text>`
- `--detect-loop`
- `--loop-interval-ms <number>`
- `--loop-count <number>`
- `--match-threshold <number>`
- `--miss-threshold <number>`
- `--simulate-audio-from-detection`
- `--allow-real-audio-from-detection`

`--real-audio` only enables real audio. To keep real audio disabled, omit `--real-audio` and set `RealAudioEnabled` to `false` in config or use the safe defaults.

## Real Audio Mode

Real audio control is enabled only when `RealAudioEnabled = true` in config or when `--real-audio` is passed:

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
- Only after separate confirmation, run guarded real detection audio against Chrome with `--real-audio --allow-real-audio-from-detection --process chrome` and confirm restore occurs on exit.

## Dependency Policy

NAudio is included only for Windows Core Audio session access in explicit real audio mode. It affects deployment by adding managed NuGet assemblies and Windows audio API access, but it does not add game integration, injection, memory reading, hooks, OCR, image-processing, model-inference, UI, or overlay features.

OCR currently uses an external Tesseract CLI process when explicitly requested. No Tesseract binaries or traineddata files are committed to the repository, and OCR output is not uploaded to any cloud service.
