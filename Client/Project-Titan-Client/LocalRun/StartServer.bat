@echo off
setlocal

net session >nul 2>nul
if not "%1"=="--no-elevate" if errorlevel 1 (
    echo Requesting administrator rights for HttpListener on http://*:8443/ ...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '--no-elevate' -Verb RunAs"
    exit /b
)

set "CLIENT_DIR=%~dp0.."
set "ROOT_DIR=%CLIENT_DIR%\..\.."
set "SERVER_DIR=%ROOT_DIR%\Server\Project-Titan"
set "SERVER_PROJECT=%SERVER_DIR%\Run.Local.All\Run.Local.All.csproj"
set "SERVER_BUILD=%CLIENT_DIR%\Builds\LocalServer"
set "SERVER_EXE=%SERVER_BUILD%\Run.Local.All.exe"
set "SERVER_DLL=%SERVER_BUILD%\Run.Local.All.dll"
set "SERVER_SETTINGS=%~dp0ServerSettings.txt"
set "CONFIG_READER=%~dp0ReadConfigValue.ps1"
set "TRIALS_LOCAL_SERVER_HOST=127.0.0.1"
set "TRIALS_SERVER_ADMIN=false"
set "TRIALS_ANTICHEAT=true"
set "TRIALS_LOOT_BOOST=1"

if exist "%CONFIG_READER%" (
    for /f "usebackq delims=" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%CONFIG_READER%" -Path "%SERVER_SETTINGS%" -Key ip -Default "127.0.0.1"`) do set "TRIALS_LOCAL_SERVER_HOST=%%A"
    for /f "usebackq delims=" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%CONFIG_READER%" -Path "%SERVER_SETTINGS%" -Key admin -Default "false"`) do set "TRIALS_SERVER_ADMIN=%%A"
    for /f "usebackq delims=" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%CONFIG_READER%" -Path "%SERVER_SETTINGS%" -Key anticheat -Default "true"`) do set "TRIALS_ANTICHEAT=%%A"
    for /f "usebackq delims=" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%CONFIG_READER%" -Path "%SERVER_SETTINGS%" -Key lootBoost -Default "1"`) do set "TRIALS_LOOT_BOOST=%%A"
)

echo Starting Trials of Titan local server...
echo LAN/Hamachi host: %TRIALS_LOCAL_SERVER_HOST%
echo Admin commands: %TRIALS_SERVER_ADMIN%
echo Anticheat: %TRIALS_ANTICHEAT%
echo Loot boost: %TRIALS_LOOT_BOOST%
echo Web port:  8443
echo Game port: 12000
echo Keep this window open while playing.
echo.

if exist "%SERVER_EXE%" (
    pushd "%SERVER_BUILD%"
    "%SERVER_EXE%"
    popd
) else if exist "%SERVER_DLL%" (
    where dotnet >nul 2>nul
    if errorlevel 1 (
        echo dotnet was not found in PATH.
        echo Install the .NET runtime to run:
        echo %SERVER_DLL%
        pause
        exit /b 1
    )

    pushd "%SERVER_BUILD%"
    dotnet "%SERVER_DLL%"
    popd
) else (
    where dotnet >nul 2>nul
    if errorlevel 1 (
        echo dotnet was not found in PATH, and no published server build exists.
        echo Run BuildServer.bat first or install the .NET SDK/runtime.
        pause
        exit /b 1
    )

    pushd "%SERVER_DIR%\Run.Local.All"
    dotnet run --project "%SERVER_PROJECT%"
    popd
)

echo.
echo Local server stopped.
pause
