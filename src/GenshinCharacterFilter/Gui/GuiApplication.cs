using GenshinCharacterFilter.Wpf;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Starts the explicit GUI control panel on an STA thread.
/// </summary>
public static class GuiApplication
{
    public static void Run(string? initialConfigPath)
    {
        Exception? uiException = null;
        Thread thread = new(() =>
        {
            try
            {
                System.Windows.Application application = new()
                {
                    ShutdownMode = System.Windows.ShutdownMode.OnMainWindowClose
                };
                WpfAppTheme theme = WpfThemeService.DetectStartupTheme();
                WpfThemeService.Apply(application, theme);
                MainWindow window = new(initialConfigPath, theme);
                application.Run(window);
            }
            catch (Exception exception)
            {
                uiException = exception;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (uiException is not null)
        {
            throw new InvalidOperationException($"GUI failed: {uiException.Message}", uiException);
        }
    }
}
