namespace GenshinCharacterFilter.Capture;

/// <summary>
/// Validates process names used by visible-window capture flows.
/// </summary>
public static class WindowCaptureProcessValidator
{
    public static void ValidateForegroundProcess(string expectedProcessName, string actualProcessName)
    {
        string expected = WindowCaptureOptions.NormalizeProcessName(expectedProcessName);
        string actual = WindowCaptureOptions.NormalizeProcessName(actualProcessName);
        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw WindowCaptureException.ForegroundWindowProcessMismatch(expected, actual);
        }
    }
}
