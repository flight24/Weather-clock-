# Weather-clock时钟天气

<img width="302" height="408" alt="image" src="https://github.com/user-attachments/assets/ed52d29c-2836-4587-9bf5-3efd18e9bca0" /> <img width="285" height="408" alt="image" src="https://github.com/user-attachments/assets/1a1e77f9-d64a-49fc-ae8f-4de60f52325b" />

一个支持全国城市的天气时钟桌面小工具，提供浏览器版和 Windows 桌面版（Electron exe）两种使用方式。

## 功能

- 实时时钟 + 日期 + 星期显示
- 当前天气：温度、体感温度、湿度、风速
- 天气图标动态动画（晴天脉冲、雨滴弹跳、闪电闪烁、雪花飘浮等）
- 天气粒子效果（雨滴下落、雪花飘落、夜间星空）
- 未来三天天气预报
- 覆盖全国 300+ 城市，省份 → 城市级联菜单选择
- 自动记忆上次选择的城市和省份
- 开机自启动（⚡ 按钮切换，蓝色高亮 = 已开启）
- 毛玻璃 UI 设计，半透明融入桌面

## 项目结构

```
weather clock/
├── 天气插件-exe修改版.html             ← 紧凑版（280×380，用于打包 exe）
├── 天气插件.html                      ← 浏览器完整版（全屏动态背景）
├── README.md
├── main.js                            ← Electron 主进程
├── preload.js                         ← 预加载脚本（开机自启 IPC）
├── package.json                       ← 依赖与构建配置
├── icon.svg / icon.ico                ← 应用图标
├── start-widget.bat                   ← 开发模式一键启动
├── build.bat                          ← 一键构建 exe
```

## 使用方法

### 方式一：桌面版 exe

直接双击 `weather clock.exe`，无需安装任何依赖。

**特性：**
- 280×380 圆角窗口
- 半透明毛玻璃效果，融入桌面
- 不在任务栏显示，不干扰其他窗口
- 关闭后自动记忆窗口位置
- 支持开机自启动

### 方式二：浏览器版

直接用浏览器打开 `天气插件.html`（全屏动态背景版）。

## 重新构建 exe

修改 HTML 或 JS 后，重新打包：

```bash
npm run build
```

## 技术栈

- 天气数据：[Open-Meteo API](https://open-meteo.com/)（免费，无需 API Key）
- 地理编码：[Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- 桌面端：[Electron](https://www.electronjs.org/)
- 构建工具：[electron-builder](https://www.electron.build/)
- 纯 HTML/CSS/JS，无前端框架依赖



# Weather-clock

A desktop weather clock widget supporting cities across China, available in both browser-based and Windows desktop (Electron exe) versions.

## Features

- Real-time clock, date, and day display
- Current weather: temperature, feels-like temperature, humidity, wind speed
- Dynamic weather icons (sun pulses, raindrops bounce, lightning flashes, snowflakes float, etc.)
- Weather particle effects (falling rain, drifting snow, night sky with stars)
- Three-day weather forecast
- Covers 300 cities nationwide, province → city cascading menu selection
- Automatically remembers the last selected city and province
- Auto-start on boot (⚡ button toggle, blue highlight = enabled)
- Frosted glass UI design, semi-transparent to blend into the desktop

## Project Structure

```
weather clock/
├── 天气插件-exe修改版.html             ← 紧凑版（280×380，用于打包 exe）
├── 天气插件.html                      ← 浏览器完整版（全屏动态背景）
├── README.md
├── main.js                            ← Electron 主进程
├── preload.js                         ← 预加载脚本（开机自启 IPC）
├── package.json                       ← 依赖与构建配置
├── icon.svg / icon.ico                ← 应用图标
├── start-widget.bat                   ← 开发模式一键启动
├── build.bat                          ← 一键构建 exe
```

## How to Use

### Option 1: Desktop EXE

Double-click `weather clock.exe` directly – no dependencies to install.

**Features:**
- 280×380 rounded-corner window
- Semi-transparent frosted glass effect that blends into the desktop
- Does not appear in the taskbar and does not interfere with other windows
- Automatically remembers window position after closing
- Supports startup on boot

### Option 2: Browser Version

Open `天气插件.html` (full-screen dynamic background version) directly in your browser.

## Rebuild the exe

After modifying the HTML or JS, repackage it:

```bash
npm run build
```

## Tech Stack

- Weather data: [Open-Meteo API](https://open-meteo.com/) (free, no API Key required)
- Geocoding: [Open-Meteo Geocoding API](https://open-meteo.com/en/docs/geocoding-api)
- Desktop: [Electron](https://www.electronjs.org/)
- Build tool: [electron-builder](https://www.electron.build/)
- Pure HTML/CSS/JS, no frontend framework dependencies
