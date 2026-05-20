using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GenshinCharacterFilter.Wpf;

/// <summary>
/// Applies optional Windows 11 backdrop hints without adding UI dependencies.
/// </summary>
public static class WpfWindowBackdrop
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSystemBackdropMainWindow = 2;

    public static void TryApply(Window window, WpfAppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(window);
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                int darkMode = theme == WpfAppTheme.Dark ? 1 : 0;
                _ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));

                int backdrop = DwmSystemBackdropMainWindow;
                _ = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref backdrop, sizeof(int));
            }
            catch
            {
                // Backdrop APIs are cosmetic and may be unavailable on older Windows builds.
            }
        };
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);
}
