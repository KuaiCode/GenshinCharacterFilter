using System.Runtime.InteropServices;

namespace GenshinCharacterFilter.Wpf;

/// <summary>
/// Retries clipboard writes that can briefly fail while another app owns the clipboard.
/// </summary>
public static class WpfClipboardRetry
{
    public static readonly TimeSpan[] DefaultRetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(150)
    ];

    public static async Task<bool> TrySetTextAsync(
        string text,
        Action<string> setText,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setText);
        delayAsync ??= static (delay, token) => Task.Delay(delay, token);

        for (int attempt = 0; attempt < DefaultRetryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TimeSpan delay = DefaultRetryDelays[attempt];
            if (delay > TimeSpan.Zero)
            {
                await delayAsync(delay, cancellationToken);
            }

            try
            {
                setText(text);
                return true;
            }
            catch (Exception exception) when (IsClipboardBusyException(exception))
            {
                if (attempt == DefaultRetryDelays.Length - 1)
                {
                    return false;
                }

                // Clipboard ownership can be transient; retry before surfacing the error in the UI log.
            }
        }

        return false;
    }

    private static bool IsClipboardBusyException(Exception exception)
    {
        return exception is COMException or ExternalException or InvalidOperationException;
    }
}
