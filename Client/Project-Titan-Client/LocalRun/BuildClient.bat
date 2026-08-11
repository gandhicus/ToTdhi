@echo off
setlocal

set "PROJECT_DIR=%~dp0.."
set "BUILD_LOG=%PROJECT_DIR%\Builds\Windows\build.log"

if not defined UNITY_EXE (
    if exist "D:\Unity\Editor\6000.3.11f1\Editor\Unity.exe" set "UNITY_EXE=D:\Unity\Editor\6000.3.11f1\Editor\Unity.exe"
)

if not defined UNITY_EXE (
    if exist "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe"
)

if not defined UNITY_EXE (
    echo Unity 6000.3.11f1 was not found.
    echo Set UNITY_EXE to your Unity.exe path and run this script again.
    echo Example:
    echo set UNITY_EXE=D:\Unity\Editor\6000.3.11f1\Editor\Unity.exe
    pause
    exit /b 1
)

echo Building Windows client with:
echo %UNITY_EXE%
echo.
echo Close this Unity project in the editor before running a batchmode build.
echo Build log: %BUILD_LOG%
echo.

"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod LocalBuild.BuildWindows -logFile "%BUILD_LOG%"
if errorlevel 1 (
    echo.
    echo Client build failed. Open the build log:
    echo %BUILD_LOG%
    pause
    exit /b 1
)

echo.
echo Client build complete:
echo %PROJECT_DIR%\Builds\Windows\TrialsOfTitan.exe
pause
