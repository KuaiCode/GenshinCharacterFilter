using System.Diagnostics;
using GenshinCharacterFilter.Audio;
using GenshinCharacterFilter.Ocr;

namespace GenshinCharacterFilter;

/// <summary>
/// Performs lightweight checks before starting OCR, capture, detection, or real-audio commands.
/// </summary>
public sealed class AppPreflightValidator
{
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, bool> _commandExists;
    private readonly Func<string, bool> _processExists;

    public AppPreflightValidator(
        Func<string, bool>? fileExists = null,
        Func<string, bool>? commandExists = null,
        Func<string, bool>? processExists = null)
    {
        _fileExists = fileExists ?? File.Exists;
        _commandExists = commandExists ?? CanResolveCommand;
        _processExists = processExists ?? IsProcessRunning;
    }

    /// <summary>
    /// Validates common runtime dependencies without starting OCR, capture, detection, or audio control.
    /// </summary>
    public RuntimePreflightResult Validate(
        AppSettings settings,
        AppCommandLineOptions options,
        AppPreflightMode mode = AppPreflightMode.Command)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(options);

        List<RuntimePreflightIssue> issues = [];
        bool ocrRequested = IsOcrRequested(options);
        bool fixedImageMode = !string.IsNullOrWhiteSpace(options.OcrInputPath);
        bool processCaptureMode = options.CaptureOnce ||
            options.CalibrateOcrRegion ||
            (options.DetectLoop && !fixedImageMode);

        if (mode == AppPreflightMode.ValidateConfig)
        {
            ValidateConfiguredPaths(settings, issues);
            return new RuntimePreflightResult(issues);
        }

        if (ocrRequested)
        {
            ValidateTesseract(settings.Ocr.TesseractExecutablePath, issues);
        }

        if (fixedImageMode)
        {
            ValidateFile("OCR preflight error", "OCR input image", options.OcrInputPath!, issues);
        }

        if (ocrRequested && !string.IsNullOrWhiteSpace(settings.Ocr.RegionConfigPath))
        {
            ValidateFile("OCR preflight error", "OCR region config", settings.Ocr.RegionConfigPath!, issues);
        }

        if (processCaptureMode)
        {
            ValidateTargetProcessName(settings.TargetProcessName, "Capture preflight error", issues);
            if (!string.IsNullOrWhiteSpace(settings.TargetProcessName) &&
                !_processExists(NormalizeProcessName(settings.TargetProcessName)))
            {
                issues.Add(new RuntimePreflightIssue(
                    "Capture preflight error",
                    $"Target process '{settings.TargetProcessName}' is not running."));
            }
        }

        if (options.AllowRealAudioFromDetection)
        {
            ValidateTargetProcessName(settings.TargetProcessName, "Audio safety error", issues);
            if (!settings.Ocr.GetOcrRegionSourceOptions().HasEffectiveRegionSource)
            {
                issues.Add(new RuntimePreflightIssue(
                    "Audio safety error",
                    "Guarded real audio detection requires an OCR region source."));
            }
        }

        return new RuntimePreflightResult(issues);
    }

    private void ValidateConfiguredPaths(AppSettings settings, List<RuntimePreflightIssue> issues)
    {
        if (!IsDefaultTesseractCommand(settings.Ocr.TesseractExecutablePath))
        {
            ValidateTesseract(settings.Ocr.TesseractExecutablePath, issues);
        }

        if (!string.IsNullOrWhiteSpace(settings.Ocr.RegionConfigPath))
        {
            ValidateFile("OCR preflight error", "OCR region config", settings.Ocr.RegionConfigPath!, issues);
        }
    }

    private void ValidateTesseract(string executablePath, List<RuntimePreflightIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            issues.Add(new RuntimePreflightIssue("OCR preflight error", "Ocr.TesseractExecutablePath cannot be empty."));
            return;
        }

        string trimmedPath = executablePath.Trim();
        bool hasDirectory = Path.GetDirectoryName(trimmedPath) is { Length: > 0 };
        if (hasDirectory)
        {
            ValidateFile("OCR preflight error", "Tesseract executable", trimmedPath, issues);
            return;
        }

        if (!_commandExists(trimmedPath))
        {
            issues.Add(new RuntimePreflightIssue(
                "OCR preflight error",
                $"Tesseract executable '{trimmedPath}' was not found on PATH."));
        }
    }

    private void ValidateFile(string category, string label, string path, List<RuntimePreflightIssue> issues)
    {
        if (!_fileExists(path))
        {
            issues.Add(new RuntimePreflightIssue(category, $"{label} not found: {path}"));
        }
    }

    private static void ValidateTargetProcessName(
        string targetProcessName,
        string category,
        List<RuntimePreflightIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(targetProcessName))
        {
            issues.Add(new RuntimePreflightIssue(category, "TargetProcessName cannot be empty."));
        }
    }

    private static bool IsOcrRequested(AppCommandLineOptions options)
    {
        return options.OcrOnce ||
            options.DetectLoop ||
            (options.DetectSpeakerOnce && options.SpeakerText is null);
    }

    private static bool IsDefaultTesseractCommand(string executablePath)
    {
        return string.Equals(
            executablePath.Trim(),
            OcrOptions.DefaultTesseractExecutablePath,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeProcessName(string processName)
    {
        return WindowsAudioMuteService.NormalizeProcessName(processName);
    }

    private static bool IsProcessRunning(string processName)
    {
        Process[] processes = [];
        try
        {
            processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }

    private static bool CanResolveCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (File.Exists(command))
        {
            return true;
        }

        string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        string pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM";
        string[] extensions = Path.HasExtension(command)
            ? [string.Empty]
            : pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (string extension in extensions)
            {
                string candidate = Path.Combine(directory, command + extension);
                if (File.Exists(candidate))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
