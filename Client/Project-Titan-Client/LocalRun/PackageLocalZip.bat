@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0PackageLocalZip.ps1"
if errorlevel 1 (
    echo.
    echo Package creation failed.
    pause
    exit /b 1
)

pause
