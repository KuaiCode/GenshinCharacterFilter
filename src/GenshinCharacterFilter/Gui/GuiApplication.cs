using System.Windows.Forms;

namespace GenshinCharacterFilter.Gui;

/// <summary>
/// Starts the minimal WinForms control panel on an STA thread.
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
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(initialConfigPath));
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
