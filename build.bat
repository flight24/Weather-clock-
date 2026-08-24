@echo off
cd /d "%~dp0"
echo 检查 .NET SDK...
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo 未检测到 .NET SDK，请先安装：https://dotnet.microsoft.com/download
    pause
    exit /b
)
echo 正在还原依赖...
dotnet restore
echo 正在构建发布版本...
dotnet publish -c Release -o publish
echo 构建完成！exe 文件在 publish\ 目录中
pause