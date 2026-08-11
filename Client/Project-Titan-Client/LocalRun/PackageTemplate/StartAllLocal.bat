@echo off
setlocal

set "ROOT_DIR=%~dp0"

echo Starting Trials of Titan local package.
echo.
echo Database and server will open in separate windows.
echo Keep those windows open while playing.
echo.

start "Trials DynamoDB Local" cmd /k ""%ROOT_DIR%StartDatabase.bat""

echo Waiting for DynamoDB Local...
timeout /t 5 /nobreak >nul

start "Trials Local Server" cmd /k ""%ROOT_DIR%StartServer.bat""

echo Waiting for local server...
timeout /t 8 /nobreak >nul

call "%ROOT_DIR%StartClient.bat"
