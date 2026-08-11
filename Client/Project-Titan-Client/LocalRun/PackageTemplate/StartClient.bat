@echo off
setlocal

set "CLIENT_EXE=%~dp0Client\TrialsOfTitan.exe"
set "LAN_CONFIG=%~dp0LocalServer.txt"
set "CONFIG_READER=%~dp0ReadConfigValue.ps1"
set "TRIALS_LOCAL_SERVER_HOST=127.0.0.1"

if exist "%CONFIG_READER%" (
    for /f "usebackq delims=" %%A in (`powershell -NoProfile -ExecutionPolicy Bypass -File "%CONFIG_READER%" -Path "%LAN_CONFIG%" -Key ip -Default "127.0.0.1" -AllowRaw`) do set "TRIALS_LOCAL_SERVER_HOST=%%A"
)

if not exist "%CLIENT_EXE%" (
    echo Client exe was not found:
    echo %CLIENT_EXE%
    pause
    exit /b 1
)

echo Starting Trials of Titan client...
echo Local server IP: %TRIALS_LOCAL_SERVER_HOST%
start "" /D "%~dp0Client" "%CLIENT_EXE%"
