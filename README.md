# GenshinCharacterFilter

GenshinCharacterFilter is a Windows console accessibility/preferences utility prototype for muting or reducing target process audio when a configured character is speaking.

Current milestone: **v0.4 OCR Text Extraction Prototype**.

The v0.1 Audio MVP, v0.2 Local JSON Configuration, and v0.3 Window Capture Prototype are implemented. v0.4 adds explicit OCR raw text extraction while preserving the safe default simulated run.

## Current Scope

- Console app only.
- Simulated speaker input.
- Target process audio filtering through `IAudioMuteService`.
- Audio modes: `Mute` and `ReduceVolume`.
- Local JSON configuration.
- CLI arguments can override JSON configuration.
- Explicit one-shot debug screenshot capture.
- Explicit one-shot OCR raw text extraction from a local image.

Out of scope: speaker detection from OCR text, automatic mute/restore based on OCR, speaker recognition from image, WPF, WinUI, overlay, masking, hotkeys, game memory access, hooks, DLL injection, game file modification, and input automation.

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

Manual OCR verification with an existing screenshot:

```powershell
dotnet run --project src/GenshinCharacterFilter/GenshinCharacterFilter.csproj -- --ocr-once --ocr-input debug-captures/capture-latest.png --ocr-lang chi_sim+eng
```

If Tesseract is not installed or the requested language data is missing, the app reports a clear OCR error. No screenshots are sent to cloud OCR services.

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

## Dependency Policy

NAudio is included only for Windows Core Audio session access in explicit real audio mode. It affects deployment by adding managed NuGet assemblies and Windows audio API access, but it does not add game integration, injection, memory reading, hooks, OCR, image-processing, model-inference, UI, or overlay features.

OCR currently uses an external Tesseract CLI process when explicitly requested. No Tesseract binaries or traineddata files are committed to the repository, and OCR output is not uploaded to any cloud service.
