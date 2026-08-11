@echo off
setlocal

set "CLIENT_DIR=%~dp0.."
set "ROOT_DIR=%CLIENT_DIR%\..\.."
set "SERVER_DIR=%ROOT_DIR%\Server\Project-Titan"
set "SERVER_PROJECT=%SERVER_DIR%\Run.Local.All\Run.Local.All.csproj"
set "SERVER_BUILD=%CLIENT_DIR%\Builds\LocalServer"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo dotnet was not found in PATH.
    echo Install the .NET SDK/runtime or use Visual Studio Developer PowerShell.
    pause
    exit /b 1
)

echo Publishing local server to:
echo %SERVER_BUILD%
echo.

dotnet publish "%SERVER_PROJECT%" -c Debug -m:1 -nr:false -p:UseSharedCompilation=false -p:DebugType=None -p:DebugSymbols=false -o "%SERVER_BUILD%"
if errorlevel 1 (
    echo.
    echo Server publish failed.
    pause
    exit /b 1
)

echo.
echo Server publish complete:
echo %SERVER_BUILD%\Run.Local.All.dll
pause
