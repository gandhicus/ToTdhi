@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SyncTitanCoreToUnity.ps1" %*
if errorlevel 1 (
    echo.
    echo TitanCore sync failed.
    pause
    exit /b 1
)

pause
