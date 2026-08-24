using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace WeatherWidget;

[ClassInterface(ClassInterfaceType.AutoDual)]
[ComVisible(true)]
public class WidgetApi
{
    private const string AppName = "WeatherWidget";
    private static readonly string AutoStartPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName, "autostart.json");

    private readonly IntPtr _windowHandle;

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    public WidgetApi(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public void StartDrag()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ReleaseCapture();
            GetCursorPos(out POINT pt);
            var lParam = (IntPtr)((pt.X & 0xFFFF) | ((pt.Y & 0xFFFF) << 16));
            SendMessage(_windowHandle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, lParam);
        });
    }

    public bool GetAutoStart()
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

    public bool SetAutoStart(bool enabled)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AutoStartPath)!);
            File.WriteAllText(AutoStartPath, $"{{\"enabled\":{enabled.ToString().ToLower()}}}");

            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WeatherWidget.exe");
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