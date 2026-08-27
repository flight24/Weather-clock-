using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;

namespace WeatherWidget;

public partial class MainWindow : Window
{
    // ---------- Win32 ----------
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

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

    // ---------- Data ----------
    private static readonly (string Prov, string[] Cities)[] ProvinceCities =
    {
        ("直辖市", new[]{"北京","上海","天津","重庆"}),
        ("河北", new[]{"石家庄","唐山","秦皇岛","邯郸","邢台","保定","张家口","承德","沧州","廊坊","衡水"}),
        ("山西", new[]{"太原","大同","阳泉","长治","晋城","朔州","晋中","运城","忻州","临汾","吕梁"}),
        ("内蒙古", new[]{"呼和浩特","包头","乌海","赤峰","通辽","鄂尔多斯","呼伦贝尔","巴彦淖尔","乌兰察布"}),
        ("辽宁", new[]{"沈阳","大连","鞍山","抚顺","本溪","丹东","锦州","营口","阜新","辽阳","盘锦","铁岭","朝阳","葫芦岛"}),
        ("吉林", new[]{"长春","吉林","四平","辽源","通化","白山","松原","白城","延吉"}),
        ("黑龙江", new[]{"哈尔滨","齐齐哈尔","鸡西","鹤岗","双鸭山","大庆","伊春","佳木斯","七台河","牡丹江","黑河","绥化"}),
        ("江苏", new[]{"南京","无锡","徐州","常州","苏州","南通","连云港","淮安","盐城","扬州","镇江","泰州","宿迁"}),
        ("浙江", new[]{"杭州","宁波","温州","嘉兴","湖州","绍兴","金华","衢州","舟山","台州","丽水"}),
        ("安徽", new[]{"合肥","芜湖","蚌埠","淮南","马鞍山","淮北","铜陵","安庆","黄山","滁州","阜阳","宿州","六安","亳州","池州","宣城"}),
        ("福建", new[]{"福州","厦门","莆田","三明","泉州","漳州","南平","龙岩","宁德"}),
        ("江西", new[]{"南昌","景德镇","萍乡","九江","新余","鹰潭","赣州","吉安","宜春","抚州","上饶"}),
        ("山东", new[]{"济南","青岛","淄博","枣庄","东营","烟台","潍坊","济宁","泰安","威海","日照","临沂","德州","聊城","滨州","菏泽"}),
        ("河南", new[]{"郑州","开封","洛阳","平顶山","安阳","鹤壁","新乡","焦作","濮阳","许昌","漯河","三门峡","南阳","商丘","信阳","周口","驻马店"}),
        ("湖北", new[]{"武汉","黄石","十堰","宜昌","襄阳","鄂州","荆门","孝感","荆州","黄冈","咸宁","随州","恩施"}),
        ("湖南", new[]{"长沙","株洲","湘潭","衡阳","邵阳","岳阳","常德","张家界","益阳","郴州","永州","怀化","娄底"}),
        ("广东", new[]{"广州","韶关","深圳","珠海","汕头","佛山","江门","湛江","茂名","肇庆","惠州","梅州","汕尾","河源","阳江","清远","东莞","中山","潮州","揭阳","云浮"}),
        ("广西", new[]{"南宁","柳州","桂林","梧州","北海","防城港","钦州","贵港","玉林","百色","贺州","河池","来宾","崇左"}),
        ("海南", new[]{"海口","三亚","儋州"}),
        ("四川", new[]{"成都","自贡","攀枝花","泸州","德阳","绵阳","广元","遂宁","内江","乐山","南充","眉山","宜宾","广安","达州","雅安","巴中","资阳"}),
        ("贵州", new[]{"贵阳","六盘水","遵义","安顺","毕节","铜仁"}),
        ("云南", new[]{"昆明","曲靖","玉溪","保山","昭通","丽江","普洱","临沧"}),
        ("西藏", new[]{"拉萨","日喀则","昌都","林芝","山南"}),
        ("陕西", new[]{"西安","铜川","宝鸡","咸阳","渭南","延安","汉中","榆林","安康","商洛"}),
        ("甘肃", new[]{"兰州","嘉峪关","金昌","白银","天水","武威","张掖","平凉","酒泉","庆阳","定西","陇南"}),
        ("青海", new[]{"西宁","海东"}),
        ("宁夏", new[]{"银川","石嘴山","吴忠","固原","中卫"}),
        ("新疆", new[]{"乌鲁木齐","克拉玛依","吐鲁番","哈密","昌吉","库尔勒","阿克苏","喀什","伊宁"}),
        ("香港", new[]{"香港"}),
        ("澳门", new[]{"澳门"}),
        ("台湾", new[]{"台北","高雄","台中","台南"})
    };

    private static readonly Dictionary<int, (string Text, string Icon)> WeatherDesc = new()
    {
        {0,("晴朗","☀️")},{1,("少云","🌤️")},{2,("多云","⛅")},{3,("阴","☁️")},
        {45,("雾","🌫️")},{48,("雾","🌫️")},{51,("毛毛雨","🌦️")},{53,("毛毛雨","🌦️")},
        {55,("毛毛雨","🌧️")},{56,("冻雨","🌧️")},{57,("冻雨","🌧️")},
        {61,("小雨","🌦️")},{63,("中雨","🌧️")},{65,("大雨","🌧️")},
        {66,("冻雨","🌧️")},{67,("冻雨","🌧️")},
        {71,("小雪","❄️")},{73,("中雪","❄️")},{75,("大雪","❄️")},{77,("雪粒","❄️")},
        {80,("阵雨","🌦️")},{81,("阵雨","🌧️")},{82,("大阵雨","🌧️")},
        {85,("阵雪","❄️")},{86,("阵雪","❄️")},
        {95,("雷阵雨","⛈️")},{96,("雷阵雨伴冰雹","⛈️")},{99,("雷阵雨伴冰雹","⛈️")}
    };

    private static readonly Dictionary<string, Color> WeatherTints = new()
    {
        ["sunny"] = Color.FromArgb(15, 255, 200, 100),
        ["cloudy"] = Color.FromArgb(13, 180, 190, 200),
        ["rainy"] = Color.FromArgb(18, 100, 160, 220),
        ["snowy"] = Color.FromArgb(15, 200, 215, 230),
        ["foggy"] = Color.FromArgb(15, 180, 190, 200),
        ["storm"] = Color.FromArgb(23, 80, 80, 140),
        ["night"] = Color.FromArgb(26, 30, 30, 70)
    };

    private static Brush Frozen(Brush b) { b.Freeze(); return b; }
    private static Geometry Frozen(Geometry g) { g.Freeze(); return g; }

    private static readonly string[] Weekdays = { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
    private static readonly string[] WeekdaysShort = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
    private static readonly Random Rnd = new();
    private static readonly HttpClient Http = CreateHttp();
    private static readonly Geolocator Geo = new();

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = System.Net.DecompressionMethods.All, MaxConnectionsPerServer = 1 });
        c.Timeout = TimeSpan.FromSeconds(15);
        c.DefaultRequestHeaders.UserAgent.ParseAdd("WeatherWidget/1.0");
        return c;
    }

    // JSON models
    internal sealed class GeoResp { [JsonPropertyName("results")] public List<GeoResult>? Results { get; set; } }
    internal sealed class GeoResult
    {
        [JsonPropertyName("latitude")] public double Lat { get; set; }
        [JsonPropertyName("longitude")] public double Lon { get; set; }
    }
    internal sealed class ForecastResp
    {
        [JsonPropertyName("current")] public CurrentBlock? Current { get; set; }
        [JsonPropertyName("hourly")] public HourlyBlock? Hourly { get; set; }
        [JsonPropertyName("daily")] public DailyBlock? Daily { get; set; }
    }
    internal sealed class CurrentBlock
    {
        [JsonPropertyName("temperature_2m")] public double Temp { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public int Humidity { get; set; }
        [JsonPropertyName("apparent_temperature")] public double Feels { get; set; }
        [JsonPropertyName("weather_code")] public int Code { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double Wind { get; set; }
        [JsonPropertyName("is_day")] public int IsDay { get; set; }
    }
    internal sealed class HourlyBlock
    {
        [JsonPropertyName("time")] public string[]? Time { get; set; }
        [JsonPropertyName("weather_code")] public int[]? Code { get; set; }
    }
    internal sealed class DailyBlock
    {
        [JsonPropertyName("time")] public string[]? Time { get; set; }
        [JsonPropertyName("temperature_2m_max")] public double[]? Tmax { get; set; }
        [JsonPropertyName("temperature_2m_min")] public double[]? Tmin { get; set; }
        [JsonPropertyName("weather_code")] public int[]? Code { get; set; }
    }
    internal sealed class ReverseGeoResp
    {
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("locality")] public string? Locality { get; set; }
    }

    // ---------- State ----------
    internal record WidgetConfig(int X, int Y, string? Prov, string? City);

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WeatherWidget", "widget-config.json");

    private readonly Dictionary<string, (double Lat, double Lon)> _coordCache = new();
    private (double Lat, double Lon)? _cachedCoords;
    private string _currentProv = "广东";
    private string _currentCity = "广州";
    private bool _menuOpen;
    private string? _lastIconType;
    private string? _lastParticleType;
    private readonly string?[] _lastMiniTypes = new string?[3];
    private System.Windows.Threading.DispatcherTimer? _clockTimer;
    private System.Windows.Threading.DispatcherTimer? _refreshTimer;
    private CancellationTokenSource? _cts;
    private string _lastDateText = "";

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyTransparency();
    }

    // ---------- Lifecycle ----------
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RestorePosition();
        LoadCityConfig();
        AutoBtn.Tag = AutoStart.IsEnabled() ? "on" : "off";

        _cts = new CancellationTokenSource();

        UpdateClock();
        _clockTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        CityBtn.Content = _currentCity + " ▾";

        _refreshTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMinutes(10) };
        _refreshTimer.Tick += async (_, _) => await FetchWeatherAsync();
        _refreshTimer.Start();

        _ = LocateDeviceAsync(isStartup: true);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveConfig();

        _clockTimer?.Stop();
        _refreshTimer?.Stop();
        _cts?.Cancel();
        _cts?.Dispose();

        ResetIconAnimation();
        RainCanvas.Stop();
        SnowCanvas.Stop();
        StarsCanvas.Stop();
        TintBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
        LocSpin.BeginAnimation(RotateTransform.AngleProperty, null);
        WeatherIconCanvas.Children.Clear();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseMenu();
        try { DragMove(); } catch { }
    }

    private void Dropdown_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

    private void ApplyTransparency()
    {
        var handle = new WindowInteropHelper(this).Handle;

        var source = HwndSource.FromHwnd(handle);
        if (source?.CompositionTarget != null)
        {
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            source.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
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

    // ---------- Config ----------
    private void LoadCityConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var cfg = JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), WeatherJsonContext.Default.WidgetConfig);
                if (!string.IsNullOrEmpty(cfg?.Prov)) _currentProv = cfg!.Prov!;
                if (!string.IsNullOrEmpty(cfg?.City)) _currentCity = cfg!.City!;
                Left = cfg?.X ?? Left;
                Top = cfg?.Y ?? Top;
            }
        }
        catch { }

        var idx = Array.FindIndex(ProvinceCities, p => p.Prov == _currentProv);
        if (idx < 0) { _currentProv = "广东"; _currentCity = "广州"; }
        else if (!ProvinceCities[idx].Cities.Contains(_currentCity))
            _currentCity = ProvinceCities[idx].Cities[0];
    }

    private void RestorePosition()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var cfg = JsonSerializer.Deserialize(File.ReadAllText(ConfigPath), WeatherJsonContext.Default.WidgetConfig);
                if (cfg != null) { Left = cfg.X; Top = cfg.Y; return; }
            }
        }
        catch { }
        Left = SystemParameters.WorkArea.Width - 300;
        Top = 80;
    }

    private void SaveConfig()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(
                new WidgetConfig((int)Left, (int)Top, _currentProv, _currentCity), WeatherJsonContext.Default.WidgetConfig));
        }
        catch { }
    }

    // ---------- Buttons ----------
    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void AutoBtn_Click(object sender, RoutedEventArgs e)
    {
        var enabled = AutoStart.SetEnabled(AutoBtn.Tag as string != "on");
        AutoBtn.Tag = enabled ? "on" : "off";
    }

    private void LocBtn_Click(object sender, RoutedEventArgs e) => _ = LocateDeviceAsync(isStartup: false);

    private void CityBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_menuOpen) { CloseMenu(); return; }
        OpenMenu();
    }

    private void MenuBack_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ShowProvinces();
    }

    // ---------- Menu ----------
    private void OpenMenu()
    {
        BuildProvinceMenu();
        ShowProvinces();
        CityDropdown.Visibility = Visibility.Visible;
        _menuOpen = true;
    }

    private void CloseMenu()
    {
        CityDropdown.Visibility = Visibility.Collapsed;
        _menuOpen = false;
    }

    private Border MakeMenuItem(string text, bool active, bool small, MouseButtonEventHandler onClick)
    {
        var border = new Border
        {
            Padding = small ? new Thickness(12, 5, 12, 5) : new Thickness(12, 5, 12, 5),
            Background = active ? ActiveBg : Brushes.Transparent,
            Cursor = Cursors.Hand
        };
        var label = new TextBlock
        {
            Text = text,
            FontSize = small ? 11 : 12,
            Foreground = active ? Brushes.White : MenuFg
        };
        border.Child = label;

        if (!active)
        {
            border.MouseEnter += (_, _) => border.Background = HoverBg;
            border.MouseLeave += (_, _) => border.Background = Brushes.Transparent;
        }
        border.MouseLeftButtonDown += (s, args) =>
        {
            args.Handled = true;
            onClick(s, args);
        };
        return border;
    }

    private void BuildProvinceMenu()
    {
        ProvinceLevel.Children.Clear();
        foreach (var (prov, _) in ProvinceCities)
        {
            var p = prov;
            ProvinceLevel.Children.Add(MakeMenuItem(p, p == _currentProv, small: false,
                (_, args) => ShowCities(p)));
        }
    }

    private void ShowProvinces()
    {
        ProvinceLevel.Visibility = Visibility.Visible;
        CityLevel.Visibility = Visibility.Collapsed;
    }

    private void ShowCities(string prov)
    {
        ProvinceLevel.Visibility = Visibility.Collapsed;
        CityLevel.Visibility = Visibility.Visible;
        CityList.Children.Clear();

        var cities = ProvinceCities.First(p => p.Prov == prov).Cities;
        foreach (var city in cities)
        {
            var c = city;
            CityList.Children.Add(MakeMenuItem(c, c == _currentCity, small: false,
                (_, args) => SelectCity(prov, c)));
        }
    }

    private void SelectCity(string prov, string city)
    {
        _currentProv = prov;
        _currentCity = city;
        _cachedCoords = null;
        SaveConfig();
        CityBtn.Content = city + " ▾";
        CloseMenu();
        _ = FetchWeatherAsync();
    }

    // ---------- Clock ----------
    private void UpdateClock()
    {
        var now = DateTime.Now;
        TimeText.Text = $"{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2}";
        var dateText = $"{now.Year}年{now.Month}月{now.Day}日 {Weekdays[(int)now.DayOfWeek]}";
        if (dateText != _lastDateText)
        {
            DateText.Text = dateText;
            _lastDateText = dateText;
        }
    }

    // ---------- Weather ----------
    private async Task<(double Lat, double Lon)?> GetCoordsAsync(string city)
    {
        if (_cts?.IsCancellationRequested == true) return null;
        if (_coordCache.TryGetValue(city, out var hit)) return hit;
        try
        {
            var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=zh";
            using var resp = await Http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var data = await JsonSerializer.DeserializeAsync(stream, WeatherJsonContext.Default.GeoResp).ConfigureAwait(false);
            var r = data?.Results?.FirstOrDefault();
            if (r != null)
            {
                _coordCache[city] = (r.Lat, r.Lon);
                return (r.Lat, r.Lon);
            }
        }
        catch { }
        return null;
    }

    private async Task FetchWeatherAsync()
    {
        if (_cts?.IsCancellationRequested == true) return;
        if (_cachedCoords.HasValue)
        {
            await FetchWeatherWithCoordsAsync(_cachedCoords.Value);
            return;
        }
        if (string.IsNullOrEmpty(_currentCity)) return;
        var coords = await GetCoordsAsync(_currentCity).ConfigureAwait(false);
        if (coords == null) { await Dispatcher.InvokeAsync(() => ErrorText.Text = "无法获取城市坐标"); return; }
        await FetchWeatherWithCoordsAsync(coords.Value);
    }

    private async Task FetchWeatherWithCoordsAsync((double Lat, double Lon) coords)
    {
        if (_cts?.IsCancellationRequested == true) return;
        try
        {
            var url = $"https://api.open-meteo.com/v1/forecast?latitude={coords.Lat}&longitude={coords.Lon}" +
                      "&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m,is_day" +
                      "&hourly=weather_code&daily=temperature_2m_max,temperature_2m_min,weather_code&forecast_days=3&timezone=auto";
            using var resp = await Http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var data = await JsonSerializer.DeserializeAsync(stream, WeatherJsonContext.Default.ForecastResp).ConfigureAwait(false);
            if (data?.Current == null || data.Daily == null) throw new InvalidOperationException();
            await Dispatcher.InvokeAsync(() => UpdateWeatherUI(data));
            await Dispatcher.InvokeAsync(() => ErrorText.Text = "");
        }
        catch
        {
            await Dispatcher.InvokeAsync(() => ErrorText.Text = "加载失败");
        }
    }

    private static string GetWeatherType(int code, int isDay)
    {
        if (isDay == 0) return "night";
        if (code == 0) return "sunny";
        if (code >= 1 && code <= 3) return "cloudy";
        if ((code >= 71 && code <= 77) || (code >= 85 && code <= 86)) return "snowy";
        if (code >= 95) return "storm";
        if ((code >= 51 && code <= 67) || (code >= 80 && code <= 82)) return "rainy";
        if (code >= 45 && code <= 48) return "foggy";
        return "cloudy";
    }

    private static string GetDayLabel(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date)) return dateStr;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var diff = date.DayNumber - today.DayNumber;
        if (diff == 0) return "今天";
        if (diff == 1) return "明天";
        if (diff == 2) return "后天";
        return WeekdaysShort[(int)date.DayOfWeek];
    }

    private void UpdateWeatherUI(ForecastResp data)
    {
        var c = data.Current!;
        var type = GetWeatherType(c.Code, c.IsDay);
        var (text, icon) = WeatherDesc.TryGetValue(c.Code, out var info) ? info : ("未知", "🌡️");

        AnimateTint(WeatherTints.GetValueOrDefault(type, Colors.Transparent));

        if (type != _lastIconType)
        {
            PlayIconAnimation(type);
            BuildIcon(type);
            _lastIconType = type;
        }
        TempRun.Text = Math.Round(c.Temp).ToString();
        WeatherDescText.Text = text == "未知" ? _currentCity : $"{_currentCity} - {text}";
        FeelsRun.Text = $"{Math.Round(c.Feels)}°C";
        HumidRun.Text = $"{c.Humidity}%";
        WindRun.Text = $"{Math.Round(c.Wind)}km/h";
        ErrorText.Text = "";

        if (type != _lastParticleType)
        {
            BuildParticles(type);
            _lastParticleType = type;
        }

        var daily = data.Daily!;
        var hourly = data.Hourly;
        for (int i = 0; i < 3 && i < daily.Time!.Length; i++)
        {
            var code = daily.Code![i];
            var target = daily.Time[i] + "T12:00";
            var idx = hourly?.Time != null ? Array.IndexOf(hourly.Time, target) : -1;
            if (idx >= 0 && hourly!.Code != null && idx < hourly.Code.Length) code = hourly.Code[idx];
            var fType = GetWeatherType(code, isDay: 1);

            switch (i)
            {
                case 0:
                    FDate0.Text = GetDayLabel(daily.Time[i]);
                    if (_lastMiniTypes[0] != fType) { BuildMiniIcon(FIcon0, fType); _lastMiniTypes[0] = fType; }
                    FTemp0.Text = $"{Math.Round(daily.Tmax![i])}°/{Math.Round(daily.Tmin![i])}°";
                    break;
                case 1:
                    FDate1.Text = GetDayLabel(daily.Time[i]);
                    if (_lastMiniTypes[1] != fType) { BuildMiniIcon(FIcon1, fType); _lastMiniTypes[1] = fType; }
                    FTemp1.Text = $"{Math.Round(daily.Tmax![i])}°/{Math.Round(daily.Tmin![i])}°";
                    break;
                case 2:
                    FDate2.Text = GetDayLabel(daily.Time[i]);
                    if (_lastMiniTypes[2] != fType) { BuildMiniIcon(FIcon2, fType); _lastMiniTypes[2] = fType; }
                    FTemp2.Text = $"{Math.Round(daily.Tmax![i])}°/{Math.Round(daily.Tmin![i])}°";
                    break;
            }
        }
        GC.Collect(0, GCCollectionMode.Optimized);
    }

    private void AnimateTint(Color to)
    {
        var anim = new ColorAnimation(to, TimeSpan.FromMilliseconds(800));
        Timeline.SetDesiredFrameRate(anim, 15);
        TintBrush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
    }

    // ---------- Icon animations ----------
    private static DoubleAnimationUsingKeyFrames KF(double seconds, params (double t, double v)[] pts)
    {
        var kf = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(seconds), RepeatBehavior = RepeatBehavior.Forever };
        Timeline.SetDesiredFrameRate(kf, 15);
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        foreach (var (t, v) in pts)
            kf.KeyFrames.Add(new EasingDoubleKeyFrame(v, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t)), ease));
        return kf;
    }

    private void ResetIconAnimation()
    {
        IconScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        IconScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        IconRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        IconTranslate.BeginAnimation(TranslateTransform.XProperty, null);
        IconTranslate.BeginAnimation(TranslateTransform.YProperty, null);
        WeatherIconCanvas.BeginAnimation(UIElement.OpacityProperty, null);
        WeatherIconCanvas.Opacity = 1;
        IconScale.ScaleX = IconScale.ScaleY = 1;
        IconRotate.Angle = 0;
        IconTranslate.X = IconTranslate.Y = 0;
    }

    private void PlayIconAnimation(string type)
    {
        ResetIconAnimation();
        switch (type)
        {
            case "sunny":
                IconScale.BeginAnimation(ScaleTransform.ScaleXProperty, KF(3.5, (0, 1), (1.75, 1.15), (3.5, 1)));
                IconScale.BeginAnimation(ScaleTransform.ScaleYProperty, KF(3.5, (0, 1), (1.75, 1.15), (3.5, 1)));
                break;
            case "cloudy":
                IconTranslate.BeginAnimation(TranslateTransform.XProperty, KF(3.5, (0, 0), (1.75, 5), (3.5, 0)));
                break;
            case "rainy":
                IconTranslate.BeginAnimation(TranslateTransform.YProperty, KF(1.2, (0, 0), (0.36, -4), (0.72, -2), (1.2, 0)));
                break;
            case "snowy":
                IconTranslate.BeginAnimation(TranslateTransform.YProperty, KF(3.5, (0, 0), (1.75, -3), (3.5, 0)));
                IconRotate.BeginAnimation(RotateTransform.AngleProperty, KF(3.5, (0, 0), (1.75, 4), (3.5, 0)));
                break;
            case "foggy":
                WeatherIconCanvas.BeginAnimation(UIElement.OpacityProperty, KF(3.5, (0, 0.6), (1.75, 1), (3.5, 0.6)));
                break;
            case "storm":
                WeatherIconCanvas.BeginAnimation(UIElement.OpacityProperty,
                    KF(2, (0, 1), (1.4, 1), (1.6, 0.15), (1.7, 1), (1.8, 0.1), (1.9, 1), (2, 1)));
                break;
            case "night":
                IconScale.BeginAnimation(ScaleTransform.ScaleXProperty, KF(4, (0, 1), (2, 1.08), (4, 1)));
                IconScale.BeginAnimation(ScaleTransform.ScaleYProperty, KF(4, (0, 1), (2, 1.08), (4, 1)));
                WeatherIconCanvas.BeginAnimation(UIElement.OpacityProperty, KF(4, (0, 0.7), (2, 1), (4, 0.7)));
                break;
        }
    }

    // ---------- Icon drawing ----------
    private static readonly Brush CloudGrad = Frozen(new LinearGradientBrush(
        Color.FromRgb(0xF5, 0xF5, 0xF5), Color.FromRgb(0x90, 0xA4, 0xAE), 90));
    private static readonly Brush CloudGradDark = Frozen(new LinearGradientBrush(
        Color.FromRgb(0x90, 0xA4, 0xAE), Color.FromRgb(0x45, 0x55, 0x65), 90));
    private static readonly Brush SunGrad = Frozen(new RadialGradientBrush(
        Color.FromRgb(0xFF, 0xF1, 0x76), Color.FromRgb(0xFF, 0x98, 0x00)));
    private static readonly Brush DropBrush = Frozen(new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)));
    private static readonly Brush BoltBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xEB, 0x3B)));
    private static readonly Brush MoonBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x82)));
    private static readonly Brush RayBrush = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xA7, 0x26)));
    private static readonly Brush FogBrush = Frozen(new SolidColorBrush(Color.FromArgb(0xCC, 0xB0, 0xBE, 0xC5)));
    private static readonly Brush GlowSun = Frozen(new SolidColorBrush(Color.FromArgb(50, 0xFF, 0xD5, 0x40)));
    private static readonly Brush GlowMoon = Frozen(new SolidColorBrush(Color.FromArgb(50, 0xFF, 0xD5, 0x4F)));
    private static readonly Brush GlowBolt = Frozen(new SolidColorBrush(Color.FromArgb(60, 0xFF, 0xEB, 0x3B)));
    private static readonly Brush CloudHl = Frozen(new SolidColorBrush(Color.FromArgb(90, 0xFF, 0xFF, 0xFF)));
    private static readonly Brush SunShine = Frozen(new SolidColorBrush(Color.FromArgb(160, 0xFF, 0xFF, 0xFF)));
    private static readonly Brush RainGrad = Frozen(new LinearGradientBrush
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(0, 1),
        GradientStops =
        {
            new GradientStop(Colors.Transparent, 0),
            new GradientStop(Color.FromArgb(128, 174, 214, 241), 1)
        }
    });
    private static readonly Brush HoverBg = Frozen(new SolidColorBrush(Color.FromArgb(26, 255, 255, 255)));
    private static readonly Brush ActiveBg = Frozen(new SolidColorBrush(Color.FromArgb(31, 255, 255, 255)));
    private static readonly Brush MenuFg = Frozen(new SolidColorBrush(Color.FromArgb(204, 255, 255, 255)));
    private static readonly Geometry StarGeo = Frozen(Geometry.Parse("M 0,-4 L 1.2,-1.2 L 4,0 L 1.2,1.2 L 0,4 L -1.2,1.2 L -4,0 L -1.2,-1.2 Z"));
    private static readonly Geometry SnowGeo = Frozen(Geometry.Parse("M 0,-3.5 L 1,-1 L 3.5,0 L 1,1 L 0,3.5 L -1,1 L -3.5,0 L -1,-1 Z"));
    private static readonly Geometry DropGeo = Frozen(Geometry.Parse("M 0,0 C 1.5,0 2,4 1,8 C 0,4 0,0 0,0 Z"));
    private static readonly Geometry MoonGeoMain = Frozen(Geometry.Parse("M 22,6 A 14,14 0 1,0 22,42 A 11,11 0 1,1 22,6 Z"));
    private static readonly Geometry MoonGeoMini = Frozen(Geometry.Parse("M 10,4 A 6,6 0 1,0 10,18 A 5,5 0 1,1 10,4 Z"));

    private static void AddGlow(Canvas c, double cx, double cy, double r, Brush brush)
    {
        var g = new Ellipse { Width = r * 2, Height = r * 2, Fill = brush };
        Canvas.SetLeft(g, cx - r); Canvas.SetTop(g, cy - r);
        c.Children.Add(g);
    }

    private static void AddCloud(Canvas c, double x, double y, Brush? fill = null)
    {
        var br = fill ?? CloudGrad;
        var b = new System.Windows.Shapes.Rectangle { Width = 40, Height = 16, RadiusX = 8, RadiusY = 8, Fill = br };
        Canvas.SetLeft(b, x + 2); Canvas.SetTop(b, y + 18); c.Children.Add(b);
        var bumps = new[] { (10, 12, 10, 10), (22, 8, 12, 12), (34, 8, 12, 12), (42, 14, 8, 8) };
        foreach (var (cx, cy, rw, rh) in bumps)
        {
            var e = new Ellipse { Width = rw * 2, Height = rh * 2, Fill = br };
            Canvas.SetLeft(e, x + cx - rw); Canvas.SetTop(e, y + cy - rh);
            c.Children.Add(e);
        }
        var hl = new Ellipse { Width = 10, Height = 8, Fill = CloudHl };
        Canvas.SetLeft(hl, x + 8); Canvas.SetTop(hl, y + 6);
        c.Children.Add(hl);
    }

    private void BuildIcon(string type)
    {
        var c = WeatherIconCanvas;
        c.Children.Clear();
        c.Opacity = 1;

        switch (type)
        {
            case "sunny":
                AddGlow(c, 22, 22, 18, GlowSun);
                var sun = new Ellipse { Width = 22, Height = 22, Fill = SunGrad };
                Canvas.SetLeft(sun, 11); Canvas.SetTop(sun, 11); c.Children.Add(sun);
                var shine = new Ellipse { Width = 8, Height = 6, Fill = SunShine };
                Canvas.SetLeft(shine, 14); Canvas.SetTop(shine, 13); c.Children.Add(shine);
                for (int i = 0; i < 8; i++)
                {
                    double a = i * Math.PI / 4;
                    double cx = 22, cy = 22, ir = 12, or = i % 2 == 0 ? 17 : 15;
                    double cosA = Math.Cos(a), sinA = Math.Sin(a), px = -sinA, py = cosA;
                    var ray = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new(cx + or * cosA, cy + or * sinA),
                            new(cx + ir * cosA + 2.8 * px, cy + ir * sinA + 2.8 * py),
                            new(cx + ir * cosA - 2.8 * px, cy + ir * sinA - 2.8 * py)
                        },
                        Fill = RayBrush
                    };
                    c.Children.Add(ray);
                }
                break;
            case "cloudy":
                AddCloud(c, 0, 0);
                break;
            case "rainy":
                AddCloud(c, 0, -4);
                for (int i = 0; i < 3; i++)
                {
                    var drop = new System.Windows.Shapes.Path { Data = DropGeo, Fill = DropBrush, Stretch = System.Windows.Media.Stretch.None };
                    Canvas.SetLeft(drop, 14 + i * 8); Canvas.SetTop(drop, 30);
                    c.Children.Add(drop);
                }
                break;
            case "snowy":
                AddCloud(c, 0, -4);
                for (int i = 0; i < 3; i++)
                {
                    var flake = new System.Windows.Shapes.Path { Data = SnowGeo, Fill = Brushes.White, Stretch = System.Windows.Media.Stretch.None };
                    Canvas.SetLeft(flake, 13 + i * 8); Canvas.SetTop(flake, 32);
                    c.Children.Add(flake);
                }
                break;
            case "storm":
                AddCloud(c, 0, -4, CloudGradDark);
                AddGlow(c, 22, 36, 10, GlowBolt);
                var bolt = new Polygon
                {
                    Points = new PointCollection { new(24, 26), new(16, 30), new(22, 30), new(16, 42), new(26, 30), new(20, 30) },
                    Fill = BoltBrush
                };
                c.Children.Add(bolt);
                break;
            case "foggy":
                for (int i = 0; i < 4; i++)
                {
                    var line = new Line { X1 = 6 + i * 2, Y1 = 12 + i * 8, X2 = 38 - i * 2, Y2 = 12 + i * 8, Stroke = FogBrush, StrokeThickness = 4, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, Opacity = 0.4 + i * 0.2 };
                    c.Children.Add(line);
                }
                break;
            case "night":
                AddGlow(c, 22, 24, 16, GlowMoon);
                var moon = new System.Windows.Shapes.Path
                {
                    Data = MoonGeoMain,
                    Fill = MoonBrush
                };
                c.Children.Add(moon);
                var starPos = new[] { (8, 10), (34, 6), (10, 34) };
                foreach (var (sx, sy) in starPos)
                {
                    var star = new System.Windows.Shapes.Path { Data = StarGeo, Fill = Brushes.White, Stretch = System.Windows.Media.Stretch.None };
                    Canvas.SetLeft(star, sx); Canvas.SetTop(star, sy);
                    c.Children.Add(star);
                }
                break;
            default:
                AddCloud(c, 0, 0);
                break;
        }
    }

    private static void AddMiniCloud(Canvas c, double x, double y, Brush? fill = null)
    {
        var br = fill ?? CloudGrad;
        var b = new System.Windows.Shapes.Rectangle { Width = 14, Height = 6, RadiusX = 3, RadiusY = 3, Fill = br };
        Canvas.SetLeft(b, x + 3); Canvas.SetTop(b, y + 6); c.Children.Add(b);
        var bumps = new[] { (6, 3, 4, 4), (10, 2, 5, 5), (14, 2, 5, 5), (17, 4, 3, 3) };
        foreach (var (cx, cy, rw, rh) in bumps)
        {
            var e = new Ellipse { Width = rw * 2, Height = rh * 2, Fill = br };
            Canvas.SetLeft(e, x + cx - rw); Canvas.SetTop(e, y + cy - rh);
            c.Children.Add(e);
        }
        var hl = new Ellipse { Width = 4, Height = 3, Fill = CloudHl };
        Canvas.SetLeft(hl, x + 5); Canvas.SetTop(hl, y + 2);
        c.Children.Add(hl);
    }

    private void BuildMiniIcon(Canvas c, string type)
    {
        c.Children.Clear();
        switch (type)
        {
            case "sunny":
                var sun = new Ellipse { Width = 10, Height = 10, Fill = SunGrad };
                Canvas.SetLeft(sun, 5); Canvas.SetTop(sun, 5); c.Children.Add(sun);
                for (int i = 0; i < 6; i++)
                {
                    double a = i * Math.PI / 3;
                    double cx = 10, cy = 10, ir = 5.5, or = i % 2 == 0 ? 8.5 : 7.5;
                    double cosA = Math.Cos(a), sinA = Math.Sin(a), px = -sinA, py = cosA;
                    var ray = new Polygon
                    {
                        Points = new PointCollection
                        {
                            new(cx + or * cosA, cy + or * sinA),
                            new(cx + ir * cosA + 1.3 * px, cy + ir * sinA + 1.3 * py),
                            new(cx + ir * cosA - 1.3 * px, cy + ir * sinA - 1.3 * py)
                        },
                        Fill = RayBrush
                    };
                    c.Children.Add(ray);
                }
                break;
            case "cloudy":
                AddMiniCloud(c, 0, 3);
                break;
            case "rainy":
                AddMiniCloud(c, 0, 1);
                for (int i = 0; i < 2; i++)
                {
                    var drop = new System.Windows.Shapes.Path { Data = DropGeo, Fill = DropBrush, Stretch = System.Windows.Media.Stretch.None };
                    var st = new ScaleTransform(0.45, 0.45);
                    Canvas.SetLeft(drop, 7 + i * 4); Canvas.SetTop(drop, 14);
                    drop.RenderTransform = st;
                    c.Children.Add(drop);
                }
                break;
            case "snowy":
                AddMiniCloud(c, 0, 1);
                for (int i = 0; i < 2; i++)
                {
                    var flake = new System.Windows.Shapes.Path { Data = SnowGeo, Fill = Brushes.White, Stretch = System.Windows.Media.Stretch.None };
                    var st = new ScaleTransform(0.55, 0.55);
                    Canvas.SetLeft(flake, 7 + i * 4); Canvas.SetTop(flake, 15);
                    flake.RenderTransform = st;
                    c.Children.Add(flake);
                }
                break;
            case "storm":
                AddMiniCloud(c, 0, 1, CloudGradDark);
                var bolt = new Polygon
                {
                    Points = new PointCollection { new(11, 12), new(8, 14), new(10, 14), new(8, 20), new(12, 14), new(9, 14) },
                    Fill = BoltBrush
                };
                c.Children.Add(bolt);
                break;
            case "foggy":
                for (int i = 0; i < 2; i++)
                {
                    var line = new Line { X1 = 4, Y1 = 7 + i * 6, X2 = 16, Y2 = 7 + i * 6, Stroke = FogBrush, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
                    c.Children.Add(line);
                }
                break;
            case "night":
                var moon = new System.Windows.Shapes.Path
                {
                    Data = MoonGeoMini,
                    Fill = MoonBrush
                };
                c.Children.Add(moon);
                var star = new System.Windows.Shapes.Path { Data = StarGeo, Fill = Brushes.White, Stretch = System.Windows.Media.Stretch.None };
                var st2 = new ScaleTransform(0.5, 0.5);
                Canvas.SetLeft(star, 16); Canvas.SetTop(star, 5);
                star.RenderTransform = st2;
                c.Children.Add(star);
                break;
            default:
                AddMiniCloud(c, 0, 0);
                break;
        }
    }

    // ---------- Particles ----------
    private void BuildParticles(string type)
    {
        RainCanvas.Stop();
        SnowCanvas.Stop();
        StarsCanvas.Stop();

        if (type is "rainy" or "storm")
        {
            RainCanvas.StartRain(type == "storm" ? 45 : 30);
        }

        if (type == "snowy")
        {
            SnowCanvas.StartSnow(18);
        }

        if (type == "night")
        {
            StarsCanvas.StartStars(22);
        }
    }

    // ---------- Location ----------
    private void SetLocLoading(bool loading)
    {
        if (loading)
        {
            LocBtn.Content = "⟳";
            var spin = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(800))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            Timeline.SetDesiredFrameRate(spin, 15);
            LocSpin.BeginAnimation(RotateTransform.AngleProperty, spin);
        }
        else
        {
            LocSpin.BeginAnimation(RotateTransform.AngleProperty, null);
            LocSpin.Angle = 0;
            LocBtn.Content = "📍";
        }
    }

    private async Task<(double Lat, double Lon)> GetGpsPositionAsync()
    {
        var op = Geo.GetGeopositionAsync(TimeSpan.Zero, TimeSpan.FromSeconds(9));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts?.Token ?? default);
        linked.CancelAfter(TimeSpan.FromSeconds(10));
        var result = await op.AsTask(linked.Token).ConfigureAwait(false);
        var pos = result.Coordinate.Point.Position;
        return (pos.Latitude, pos.Longitude);
    }

    private async Task<string> ReverseGeocodeAsync(double lat, double lon)
    {
        if (_cts?.IsCancellationRequested == true) return "当前位置";
        try
        {
            var url = $"https://api.bigdatacloud.net/data/reverse-geocode-client?latitude={lat}&longitude={lon}&localityLanguage=zh";
            using var resp = await Http.GetAsync(url).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var data = await JsonSerializer.DeserializeAsync(stream, WeatherJsonContext.Default.ReverseGeoResp).ConfigureAwait(false);
            var name = !string.IsNullOrEmpty(data?.City) ? data.City
                     : !string.IsNullOrEmpty(data?.Locality) ? data.Locality
                     : "当前位置";
            return name.EndsWith("市") ? name[..^1] : name;
        }
        catch
        {
            return "当前位置";
        }
    }

    private async Task LocateDeviceAsync(bool isStartup)
    {
        if (_cts?.IsCancellationRequested == true) return;
        if (!isStartup) SetLocLoading(true);
        try
        {
            var coords = await GetGpsPositionAsync().ConfigureAwait(false);
            _cachedCoords = coords;
            var name = await ReverseGeocodeAsync(coords.Lat, coords.Lon).ConfigureAwait(false);
            _currentProv = "";
            _currentCity = $"当前位置（{name}）";
            SaveConfig();
            await Dispatcher.InvokeAsync(() => CityBtn.Content = "📍 " + name + " ▾");
            await FetchWeatherWithCoordsAsync(coords);
        }
        catch
        {
            await FetchWeatherAsync();
        }
        finally
        {
            if (!isStartup) await Dispatcher.InvokeAsync(() => SetLocLoading(false));
        }
    }

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(GeoResp))]
    [JsonSerializable(typeof(ForecastResp))]
    [JsonSerializable(typeof(ReverseGeoResp))]
    [JsonSerializable(typeof(WidgetConfig))]
    internal partial class WeatherJsonContext : JsonSerializerContext
    {
    }
}

internal class ParticleLayer : Canvas
{
    private struct RainDrop { public double X, Y, Vy; }
    private struct SnowFlake { public double X, Y, Vy, Size; }
    private struct Star { public double X, Y, Opacity; }

    private RainDrop[]? _rain;
    private SnowFlake[]? _snow;
    private Star[]? _stars;
    private System.Windows.Threading.DispatcherTimer? _timer;

    private static readonly LinearGradientBrush RainGrad;
    private static readonly Random Rnd = new();

    static ParticleLayer()
    {
        RainGrad = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = { new GradientStop(Colors.Transparent, 0), new GradientStop(Color.FromArgb(128, 174, 214, 241), 1) }
        };
        RainGrad.Freeze();
    }

    internal void StartRain(int count)
    {
        _rain = new RainDrop[count];
        for (int i = 0; i < count; i++)
        {
            var dur = 0.4 + Rnd.NextDouble() * 0.8;
            _rain[i] = new RainDrop
            {
                X = Rnd.NextDouble() * 280,
                Y = Rnd.NextDouble() * 390,
                Vy = 390 / dur / 15
            };
        }
        StartTimer();
    }

    internal void StartSnow(int count)
    {
        _snow = new SnowFlake[count];
        for (int i = 0; i < count; i++)
        {
            var dur = 3 + Rnd.NextDouble() * 4;
            _snow[i] = new SnowFlake
            {
                X = Rnd.NextDouble() * 280,
                Y = Rnd.NextDouble() * 390,
                Vy = 390 / dur / 15,
                Size = 1.5 + Rnd.NextDouble() * 2.5
            };
        }
        StartTimer();
    }

    internal void StartStars(int count)
    {
        _stars = new Star[count];
        for (int i = 0; i < count; i++)
        {
            _stars[i] = new Star
            {
                X = Rnd.NextDouble() * 280,
                Y = Rnd.NextDouble() * 228,
                Opacity = 0.15 + Rnd.NextDouble() * 0.45
            };
        }
        InvalidateVisual();
    }

    internal void Stop()
    {
        _timer?.Stop();
        _rain = null;
        _snow = null;
        _stars = null;
        InvalidateVisual();
    }

    private void StartTimer()
    {
        if (_timer == null)
        {
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(67) };
            _timer.Tick += (_, _) =>
            {
                Update();
                InvalidateVisual();
            };
        }
        _timer.Start();
    }

    private void Update()
    {
        if (_rain != null)
        {
            for (int i = 0; i < _rain.Length; i++)
            {
                _rain[i].Y += _rain[i].Vy;
                if (_rain[i].Y > 390) { _rain[i].Y = -12; _rain[i].X = Rnd.NextDouble() * 280; }
            }
        }
        if (_snow != null)
        {
            for (int i = 0; i < _snow.Length; i++)
            {
                _snow[i].Y += _snow[i].Vy;
                if (_snow[i].Y > 390) { _snow[i].Y = -10; _snow[i].X = Rnd.NextDouble() * 280; }
            }
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_rain != null)
        {
            foreach (var d in _rain)
                dc.DrawRectangle(RainGrad, null, new Rect(d.X, d.Y, 1.5, 12));
        }
        if (_snow != null)
        {
            foreach (var f in _snow)
                dc.DrawEllipse(Brushes.White, null, new Point(f.X, f.Y), f.Size, f.Size);
        }
        if (_stars != null)
        {
            foreach (var s in _stars)
            {
                dc.PushOpacity(s.Opacity);
                dc.DrawEllipse(Brushes.White, null, new Point(s.X, s.Y), 1, 1);
                dc.Pop();
            }
        }
    }
}
