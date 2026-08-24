using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace WeatherWidget;

public partial class MainWindow : Window
{
    private const string AppName = "WeatherWidget";
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        AppName, "widget-config.json");

    private record WidgetConfig(int X, int Y);

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const uint LWA_ALPHA = 0x2;

    private const int WCA_ACCENT_POLICY = 19;
    private const int ACCENT_ENABLE_BLURBEHIND = 3;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (s, e) => ApplyTransparency();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RestorePosition();
        await InitializeWebView();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SavePosition();
    }

    private void ApplyTransparency()
    {
        var handle = new WindowInteropHelper(this).Handle;

        var source = HwndSource.FromHwnd(handle);
        if (source?.CompositionTarget != null)
        {
            source.CompositionTarget.BackgroundColor = System.Windows.Media.Colors.Transparent;
        }

        var accent = new AccentPolicy
        {
            AccentState = ACCENT_ENABLE_BLURBEHIND,
            AccentFlags = 0,
            GradientColor = 0x01000000,
            AnimationId = 0
        };

        var data = new WindowCompositionAttributeData
        {
            Attribute = WCA_ACCENT_POLICY,
            Data = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>()),
            SizeOfData = Marshal.SizeOf<AccentPolicy>()
        };
        Marshal.StructureToPtr(accent, data.Data, false);
        SetWindowCompositionAttribute(handle, ref data);
        Marshal.FreeHGlobal(data.Data);

        int cornerPref = DWMWCP_ROUND;
        DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

        GetWindowRect(handle, out RECT rect);
        var rgn = CreateRoundRectRgn(0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, 32, 32);
        SetWindowRgn(handle, rgn, true);
    }

    private void RestorePosition()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<WidgetConfig>(json);
                if (config != null)
                {
                    Left = config.X;
                    Top = config.Y;
                    return;
                }
            }
        }
        catch { }

        var screenW = SystemParameters.WorkArea.Width;
        Left = screenW - 300;
        Top = 80;
    }

    private void SavePosition()
    {
        try
        {
            var config = new WidgetConfig((int)Left, (int)Top);
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config));
        }
        catch { }
    }

    private async Task InitializeWebView()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "天气插件");
        Directory.CreateDirectory(tempDir);

        var htmlPath = Path.Combine(tempDir, "天气插件.html");
        if (!File.Exists(htmlPath))
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("天气插件.html"));
            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                using var fileStream = File.Create(htmlPath);
                stream!.CopyTo(fileStream);
            }
        }
        if (!File.Exists(htmlPath))
        {
            webView.CoreWebView2.NavigateToString("<html><body style='color:white;text-align:center;padding-top:100px;font-family:sans-serif'><p>找不到天气插件.html</p></body></html>");
            return;
        }

        var options = new CoreWebView2EnvironmentOptions(
            "--disable-gpu --disable-software-rasterizer --disable-background-networking --disable-component-update --disable-default-apps --disable-extensions --disable-sync --no-first-run --no-default-browser-check --disable-popup-blocking --disable-hang-monitor --disable-prompt-on-repost --disable-domain-reliability --disable-features=msEdgeAI,msEdgeAIChat,msEdgeCopilot,msEdgeAIFeatures,msSearch,msEdgeTranslate");
        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: Path.Combine(tempDir, "WebView2"),
            options: options);
        await webView.EnsureCoreWebView2Async(env);

        webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        webView.DefaultBackgroundColor = Color.Transparent;

        var handle = new WindowInteropHelper(this).Handle;
        webView.CoreWebView2.AddHostObjectToScript("api", new WidgetApi(handle));

        _ = webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            window.electronAPI = {
                getAutoStart: () => Promise.resolve(window.chrome.webview.hostObjects.sync.api.GetAutoStart()),
                setAutoStart: (enabled) => Promise.resolve(window.chrome.webview.hostObjects.sync.api.SetAutoStart(enabled))
            };
            if (!window._dragSetup) {
                window._dragSetup = true;
                document.addEventListener('mousedown', function(e) {
                    var target = e.target;
                    var isInteractive = target.closest('button, .menu-item, .menu-back, .city-dropdown, .city-dropdown *, select, input');
                    if (!isInteractive) {
                        window.chrome.webview.hostObjects.sync.api.StartDrag();
                    }
                });
            }
        ");

        webView.CoreWebView2.WindowCloseRequested += (s, e) =>
        {
            Dispatcher.Invoke(() => Close());
        };

        webView.Source = new Uri(htmlPath);
    }
}