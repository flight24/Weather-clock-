using System.IO;
using Microsoft.Win32;

namespace WeatherWidget;

public static class AutoStart
{
    private const string AppName = "WeatherWidget";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enabled)
            {
                var exePath = Environment.ProcessPath
                    ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "天气插件.exe");
                key?.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key?.DeleteValue(AppName, false);
            }
            return enabled;
        }
        catch
        {
            return false;
        }
    }
}
