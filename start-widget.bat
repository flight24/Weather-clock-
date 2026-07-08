@echo off
cd /d "%~dp0"
echo 检查 Node.js...
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo 未检测到 Node.js，请先安装：https://nodejs.org/
    pause
    exit /b
)
if not exist "node_modules" (
    echo 正在安装依赖...
    call npm install
)
echo 启动桌面天气插件...
call npm start
