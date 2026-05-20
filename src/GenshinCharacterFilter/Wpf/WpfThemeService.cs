using Microsoft.Win32;
using System.Windows;

namespace GenshinCharacterFilter.Wpf;

public enum WpfAppTheme
{
    Light,
    Dark
}

/// <summary>
/// Loads startup theme resources for the WPF shell.
/// </summary>
public static class WpfThemeService
{
    public static WpfAppTheme DetectStartupTheme()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);

            return value is int intValue && intValue == 0
                ? WpfAppTheme.Dark
                : WpfAppTheme.Light;
        }
        catch
        {
            return WpfAppTheme.Light;
        }
    }

    public static void Apply(System.Windows.Application application, WpfAppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(application);
        application.Resources.MergedDictionaries.Clear();
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Wpf/Themes/{theme}Theme.xaml", UriKind.Absolute)
        });
        application.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/Wpf/ModernTheme.xaml", UriKind.Absolute)
        });
    }
}
