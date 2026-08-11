@echo off
setlocal

net session >nul 2>nul
if not "%1"=="--no-elevate" if errorlevel 1 (
    echo Requesting administrator rights for HttpListener on http://*:8443/ ...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '--no-elevate' -Verb RunAs"
    exit /b
)

set "ROOT_DIR=%~dp0"
set "SERVER_DIR=%ROOT_DIR%Server"
set "SERVER_EXE=%SERVER_DIR%\Run.Local.All.exe"
set "SERVER_DLL=%SERVER_DIR%\Run.Local.All.dll"
set "DOTNET_EXE=%ROOT_DIR%Runtime\DotNet\dotnet.exe"
set "SERVER_SETTINGS=%ROOT_DIR%ServerSettings.txt"
set "CONFIG_READER=%ROOT_DIR%ReadConfigValue.ps1"
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

if not exist "%DOTNET_EXE%" (
    set "DOTNET_EXE=dotnet"
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
    pushd "%SERVER_DIR%"
    "%SERVER_EXE%"
    popd
) else if exist "%SERVER_DLL%" (
    where "%DOTNET_EXE%" >nul 2>nul
    if errorlevel 1 if not exist "%DOTNET_EXE%" (
        echo dotnet was not found in PATH.
        echo Install the .NET Core runtime required by this server package,
        echo include a portable runtime at Runtime\DotNet,
        echo or use a package that includes Server\Run.Local.All.exe.
        pause
        exit /b 1
    )

    pushd "%SERVER_DIR%"
    "%DOTNET_EXE%" "%SERVER_DLL%"
    popd
) else (
    echo Server executable was not found:
    echo %SERVER_EXE%
    echo %SERVER_DLL%
    pause
    exit /b 1
)

echo.
echo Local server stopped.
pause
