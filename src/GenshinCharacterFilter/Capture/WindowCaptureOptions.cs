namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Options for a one-shot debug capture of a target game window.
/// </summary>
public sealed class WindowCaptureOptions
{
    public const string DefaultOutputDirectory = "debug-captures";
    public const string DefaultOutputFileName = "capture-latest.png";
    public const int DefaultCaptureDelayMs = 500;
    public const int MaxCaptureDelayMs = 5000;
    public const string TempCaptureDirectoryName = "GenshinCharacterFilter";

    /// <summary>
    /// Gets or sets the process name used to locate the target window.
    /// </summary>
    public string TargetProcessName { get; set; } = "GenshinImpact";

    /// <summary>
    /// Gets or sets an optional region relative to the target window.
    /// </summary>
    public CaptureRegion? CaptureRegion { get; set; }

    /// <summary>
    /// Gets or sets the directory where debug screenshots are saved.
    /// </summary>
    public string OutputDirectory { get; set; } = DefaultOutputDirectory;

    /// <summary>
    /// Gets or sets the output file name for the debug screenshot.
    /// </summary>
    public string OutputFileName { get; set; } = DefaultOutputFileName;

    /// <summary>
    /// Gets or sets the delay after foreground activation before pixels are captured.
    /// </summary>
    public int CaptureDelayMs { get; set; } = DefaultCaptureDelayMs;

    /// <summary>
    /// Gets or sets whether capture should save the stable debug screenshot path.
    /// </summary>
    public bool SaveDebugImage { get; set; } = true;

    /// <summary>
    /// Validates options before capture starts.
    /// </summary>
    public void Validate()
    {
        NormalizeProcessName(TargetProcessName);

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new ArgumentException("Capture output directory cannot be empty.", nameof(OutputDirectory));
        }

        if (string.IsNullOrWhiteSpace(OutputFileName))
        {
            throw new ArgumentException("Capture output file name cannot be empty.", nameof(OutputFileName));
        }

        string extension = Path.GetExtension(OutputFileName);
        if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Capture output file name must use the .png extension.", nameof(OutputFileName));
        }

        ValidateCaptureDelayMs(CaptureDelayMs);
    }

    /// <summary>
    /// Validates the foreground activation delay range.
    /// </summary>
    public static void ValidateCaptureDelayMs(int captureDelayMs)
    {
        if (captureDelayMs is < 0 or > MaxCaptureDelayMs)
        {
            throw new ArgumentOutOfRangeException(nameof(captureDelayMs), $"Capture delay must be between 0 and {MaxCaptureDelayMs} ms.");
        }
    }

    /// <summary>
    /// Returns a process name suitable for Process.GetProcessesByName.
    /// </summary>
    public static string NormalizeProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            throw new ArgumentException("Target process name cannot be empty.", nameof(processName));
        }

        string fileName = Path.GetFileName(processName.Trim());
        return string.Equals(Path.GetExtension(fileName), ".exe", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(fileName)
            : fileName;
    }

    /// <summary>
    /// Builds the full output path for the debug screenshot.
    /// </summary>
    public string GetOutputPath()
    {
        Validate();
        return Path.GetFullPath(Path.Combine(OutputDirectory.Trim(), OutputFileName.Trim()));
    }

    /// <summary>
    /// Builds the file path used by the capture implementation.
    /// </summary>
    public string GetCaptureOutputPath()
    {
        return SaveDebugImage
            ? GetOutputPath()
            : Path.Combine(GetTempCaptureDirectory(), $"capture-{Guid.NewGuid():N}.png");
    }

    /// <summary>
    /// Returns the temp directory used for non-debug realtime capture inputs.
    /// </summary>
    public static string GetTempCaptureDirectory()
    {
        return Path.Combine(Path.GetTempPath(), TempCaptureDirectoryName);
    }
}
