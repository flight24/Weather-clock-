# Weather-clock时钟天气

<img width="302" height="408" alt="image" src="https://github.com/user-attachments/assets/ed52d29c-2836-4587-9bf5-3efd18e9bca0" /> <img width="285" height="408" alt="image" src="https://github.com/user-attachments/assets/1a1e77f9-d64a-49fc-ae8f-4de60f52325b" />

一个支持全国城市的天气时钟桌面小工具，使用纯原生 WPF (.NET 9) 构建。

## 功能

- 实时时钟 + 日期 + 星期显示
- 当前天气：温度、体感温度、湿度、风速
- 天气图标动态动画（晴天脉冲、雨滴弹跳、闪电闪烁、雪花飘浮等）
- 天气粒子效果（雨滴下落、雪花飘落、夜间星空）
- 未来三天天气预报
- 覆盖全国 300+ 城市，省份 → 城市级联菜单选择
- GPS 自动定位（📍 按钮，启动时自动定位一次，失败则回退到上次城市）
- 反向地理编码显示定位城市名
- 自动记忆上次选择的城市和省份
- 开机自启动（⚡ 按钮切换，蓝色高亮 = 已开启）
- 半透明毛玻璃 UI 设计，融入桌面
- 窗口拖动（在非按钮区域按住拖动）
- 全矢量卡通图标（主图标 + 预报小图标），无图片资源依赖

## 项目结构

```
weather-clock-wpf/
├── App.xaml / App.xaml.cs             ← 应用程序入口
├── MainWindow.xaml                    ← WPF 窗口布局（时钟、天气、粒子、菜单）
├── MainWindow.xaml.cs                 ← 全部逻辑（天气、GPS、动画、粒子、菜单、配置）
├── WidgetApi.cs                       ← 开机自启注册表
├── Properties/AssemblyInfo.cs         ← 程序集信息
├── WeatherWidget.csproj              ← 项目配置（.NET 9）
├── icon.ico                          ← 应用图标
└── README.md
```

## 使用方法

直接双击 `publish\天气插件.exe`，无需安装任何依赖。

**特性：**
- 280×380 圆角窗口
- 半透明毛玻璃效果，融入桌面
- 启动时自动 GPS 定位，失败则回退到上次城市
- 不在任务栏显示，不干扰其他窗口
- 关闭后自动记忆窗口位置
- 支持开机自启动

## 重新构建 exe

### 环境要求

- .NET 9 SDK
- Windows 10 1809+ / Windows 11 x64（需支持 `Windows.Devices.Geolocation`）

### 构建步骤

```bash
# 手动构建
dotnet publish -c Release -o publish
```

输出文件：`publish\天气插件.exe`（单文件自包含绿色版）

## 技术栈

- 天气数据：[Open-Meteo API](https://open-meteo.com/)（免费，无需 API Key）
- 地理编码：[Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- 反向地理编码：[BigDataCloud Reverse Geocode](https://www.bigdatacloud.com/)（免费无注册，国内直连）
- GPS 定位：`Windows.Devices.Geolocation.Geolocator`（WinRT API）
- 桌面端：纯原生 WPF (.NET 9)
- 毛玻璃效果：DWM `SetWindowCompositionAttribute` + `ACCENT_ENABLE_BLURBEHIND`
- 天气图标：WPF 矢量绘制（Canvas + Shapes + Storyboard 动画）
- 粒子效果：Canvas + DoubleAnimation（雨/雪/星空）



# Weather-clock

A desktop weather clock widget supporting cities across China, built with pure native WPF (.NET 9).

## Features

- Real-time clock, date, and day display
- Current weather: temperature, feels-like temperature, humidity, wind speed
- Dynamic weather icons (sun pulses, raindrops bounce, lightning flashes, snowflakes float, etc.)
- Weather particle effects (falling rain, drifting snow, night sky with stars)
- Three-day weather forecast
- Covers 300+ cities nationwide, province → city cascading menu selection
- GPS auto-location (📍 button, auto-locates once on startup, falls back to last city on failure)
- Reverse geocoding to display the located city name
- Automatically remembers the last selected city and province
- Auto-start on boot (⚡ button toggle, blue highlight = enabled)
- Semi-transparent frosted glass UI design, blends into the desktop
- Window dragging (hold and drag on non-button areas)
- Fully vector cartoon icons (main + forecast mini icons), no image assets

## Project Structure

```
weather-clock-wpf/
├── App.xaml / App.xaml.cs             ← Application entry point
├── MainWindow.xaml                    ← WPF window layout (clock, weather, particles, menu)
├── MainWindow.xaml.cs                 ← All logic (weather, GPS, animation, particles, menu, config)
├── WidgetApi.cs                       ← Auto-start registry
├── Properties/AssemblyInfo.cs         ← Assembly info
├── WeatherWidget.csproj              ← Project config (.NET 9)
├── icon.ico                          ← App icon
└── README.md
```

## How to Use

Double-click `publish\天气插件.exe` directly – no dependencies to install.

**Features:**
- 280×380 rounded-corner window
- Semi-transparent frosted glass effect that blends into the desktop
- Auto GPS-location on startup, falls back to last saved city on failure
- Does not appear in the taskbar and does not interfere with other windows
- Automatically remembers window position after closing
- Supports startup on boot

## Rebuild the exe

### Requirements

- .NET 9 SDK
- Windows 10 1809+ / Windows 11 x64 (requires `Windows.Devices.Geolocation` support)

### Build Steps

```bash
# Manual build
dotnet publish -c Release -o publish
```

Output: `publish\天气插件.exe` (single-file self-contained)

## Tech Stack

- Weather data: [Open-Meteo API](https://open-meteo.com/) (free, no API Key required)
- Geocoding: [Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- Reverse geocoding: [BigDataCloud Reverse Geocode](https://www.bigdatacloud.com/) (free, no registration, direct access in China)
- GPS location: `Windows.Devices.Geolocation.Geolocator` (WinRT API)
- Desktop: Pure native WPF (.NET 9)
- Frosted glass effect: DWM `SetWindowCompositionAttribute` + `ACCENT_ENABLE_BLURBEHIND`
- Weather icons: WPF vector drawing (Canvas + Shapes + Storyboard animations)
- Particle effects: Canvas + DoubleAnimation (rain/snow/stars)
