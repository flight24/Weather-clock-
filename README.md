# Weather-clock时钟天气

<img width="281" height="380" alt="image" src="https://github.com/user-attachments/assets/f2108ea2-3886-42fd-8579-c7341a93fef5" />
<img width="866" height="757" alt="image" src="https://github.com/user-attachments/assets/3f5dce4b-d672-4b73-b213-4258e135d61f" />

一个支持全国城市的天气时钟桌面小工具，提供浏览器版和 Windows 桌面版（Electron exe）两种使用方式。

## 功能

- 实时时钟 + 日期 + 星期显示
- 当前天气：温度、体感温度、湿度、风速
- 天气图标动态动画（晴天脉冲、雨滴弹跳、闪电闪烁、雪花飘浮等）
- 动态天气背景（晴天金色、雨天深蓝、雪天灰白、雷暴暗紫、夜间星空）
- 未来三天天气预报
- 覆盖全国 300+ 城市，省份 → 城市级联选择
- 自动记忆上次选择的城市
- 毛玻璃 UI 设计

## 使用方法

### 方式一：桌面版 exe

直接双击 `weather clock.exe`，无需安装任何依赖。

**特性：**
- 280×380 圆角窗口
- 半透明毛玻璃效果，融入桌面
- 不在任务栏显示，不干扰其他窗口
- 关闭后自动记忆窗口位置

### 方式二：浏览器版

直接用浏览器打开 `天气插件.html`（全屏动态背景版）。

## 技术栈

- 天气数据：[Open-Meteo API](https://open-meteo.com/)（免费，无需 API Key）
- 地理编码：[Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- 桌面端：[Electron](https://www.electronjs.org/)
- 构建工具：[electron-builder](https://www.electron.build/)
- 纯 HTML/CSS/JS，无前端框架依赖



# Weather-clock

A desktop weather clock widget supporting cities across China, available in both browser-based and Windows desktop (Electron exe) versions.

## Features

- Real-time clock + date + weekday display
- Current weather: temperature, feels-like temperature, humidity, wind speed
- Dynamic weather icons with animations (sunny pulse, raindrop bounce, lightning flash, snow flurry, etc.)
- Dynamic weather backgrounds (golden for sunny, deep blue for rainy, greyish white for snowy, dark purple for thunderstorm, starry night sky)
- 3-day weather forecast
- Covers 300+ cities across China, with province → city cascading selection
- Automatically remembers the last selected city
- Frosted glass UI design

## How to Use

### Option 1: Desktop EXE

Double-click `weather clock.exe` directly – no dependencies to install.

**Features:**
- 280×380 rounded-corner window
- Semi-transparent frosted glass effect that blends into the desktop
- Does not appear in the taskbar and does not interfere with other windows
- Automatically remembers window position after closing

### Option 2: Browser Version

Open `天气插件.html` (full-screen dynamic background version) directly in your browser.

## Tech Stack

- Weather data: [Open-Meteo API](https://open-meteo.com/) (free, no API Key required)
- Geocoding: [Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- Desktop: [Electron](https://www.electronjs.org/)
- Build tool: [electron-builder](https://www.electron.build/)
- Pure HTML/CSS/JS, no frontend framework dependencies
